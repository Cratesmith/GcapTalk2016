using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;

#if UNITY_EDITOR
using System.IO;
using System.Linq;
#endif

public class ManagerAttributeCache : ResourceSingleton<ManagerAttributeCache>,
ISerializationCallbackReceiver
{
    [System.Serializable] 
    public struct SerializedManagerAttributes
    {       
        public SerializedManagerAttributes(ManagerAttributes source)
        {
            Assert.IsNotNull(source.managerType);
            managerTypeName = source.managerType.Name;
            isGlobalOnly    = source.isAlwaysGlobal;
            Assert.IsNotNull(source.managerDepenedencyTypes);
            managerDependencyTypeNames = new string[source.managerDepenedencyTypes.Length];
            for(int i=0; i<managerDependencyTypeNames.Length; ++i)
            {
                managerDependencyTypeNames[i] = source.managerDepenedencyTypes[i].Name;
            }
        }
        public string           managerTypeName;
        public bool             isGlobalOnly;
        public string[]         managerDependencyTypeNames;
    }
    [SerializeField] SerializedManagerAttributes[] m_serializedAttributes = new SerializedManagerAttributes[0];

    public struct ManagerAttributes
    {
        #if UNITY_EDITOR
        public ManagerAttributes(System.Type type) 
        {
            Assert.IsNotNull(type);
            Assert.IsTrue(typeof(Manager).IsAssignableFrom(type));
            managerType = type;
            managerDependencyTypesLookup = null;

            isAlwaysGlobal = type.GetCustomAttributes(typeof(ManagerAlwaysGlobalAttribute), true).Length > 0;

            managerDepenedencyTypes = type.GetCustomAttributes(typeof(ManagerDependencyAttribute), true)
                .Cast<ManagerDependencyAttribute>()
                .SelectMany(x=>x.GetManagerDependencies())
                .Distinct()
                .ToArray();        

            managerDependencyTypesLookup = new HashSet<System.Type>(managerDepenedencyTypes);
        }
        #endif

        public ManagerAttributes(SerializedManagerAttributes source)
        {
            managerType  = null;
            isAlwaysGlobal = source.isGlobalOnly;
            managerDepenedencyTypes = null;
            managerDependencyTypesLookup = null;

            var assembly = GetType().Assembly;
            managerType  = !string.IsNullOrEmpty(source.managerTypeName) ? assembly.GetType(source.managerTypeName):null;
            var list = new List<System.Type>();
            if(managerDepenedencyTypes!=null)
            {
                for(int i=0; i<managerDepenedencyTypes.Length; ++i)
                {
                    if(string.IsNullOrEmpty(source.managerDependencyTypeNames[i])) continue;
                    var type = assembly.GetType(source.managerDependencyTypeNames[i]);
                    if(type==null) continue;
                    list.Add(type);
                }
            }
            managerDepenedencyTypes = list.ToArray(); 

            managerDependencyTypesLookup = new HashSet<System.Type>(managerDepenedencyTypes);
        }

        public System.Type          managerType;
        public bool                 isAlwaysGlobal;
        public System.Type[]        managerDepenedencyTypes;
        public HashSet<System.Type> managerDependencyTypesLookup;

    }
    Dictionary<System.Type, ManagerAttributes> m_attributes = new Dictionary<System.Type, ManagerAttributes>();


    #region ISerializationCallbackReceiver implementation
    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
        int numTypes = m_attributes.Count;
        m_serializedAttributes = new SerializedManagerAttributes[numTypes];
        var e = m_attributes.Values.GetEnumerator();
        int i = 0;
        while(e.MoveNext())
        {
            m_serializedAttributes[i] = new SerializedManagerAttributes(e.Current);
            ++i;
        }
    }
    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        #if !UNITY_EDITOR
        m_attributes.Clear();
        for(int i=0; i<m_serializedAttributes.Length; ++i)
        {
            var attributes = new ManagerAttributes(m_serializedAttributes[i]);
            m_attributes.Add(attributes.managerType, attributes);
        }
        #endif
    }
    #endregion

    #if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    static void ProcessScripts()
    {
        var types = new string[] {".cs", ".js"};

        var allScriptPaths = Directory.GetFiles("Assets", "*.*", SearchOption.AllDirectories)
            .Where(s => types.Any(x=>s.EndsWith(x, System.StringComparison.CurrentCultureIgnoreCase)))
            .ToArray();
                   
        instance.m_attributes.Clear();

        for(int i=0;i<allScriptPaths.Length; ++i)
        {
            UnityEditor.MonoScript script = UnityEditor.AssetDatabase.LoadAssetAtPath(allScriptPaths[i], typeof(UnityEditor.MonoScript)) as UnityEditor.MonoScript;
            if(!script || script.GetClass()==null) continue;
            if(!typeof(Manager).IsAssignableFrom(script.GetClass())) continue;

            var type = script.GetClass();  
            instance.m_attributes[type] = new ManagerAttributes(type);
        }

        instance.hideFlags = HideFlags.NotEditable;

        var so = new UnityEditor.SerializedObject(instance);
        so.Update();
        so.ApplyModifiedProperties();
    }
    #endif

    public static System.Type[] GetManagerDependencies(System.Type managerType)
    {
        Assert.IsNotNull(managerType);
        Assert.IsTrue(typeof(Manager).IsAssignableFrom(managerType));
        ManagerAttributes attribs;
        if(instance.m_attributes.TryGetValue(managerType, out attribs))
        {
            return attribs.managerDepenedencyTypes;
        }

        return new System.Type[0];
    }

    public static bool DoesManagerDependOn(System.Type managerType, System.Type dependsOn)
    {
        Assert.IsNotNull(managerType);
        Assert.IsNotNull(dependsOn);
        Assert.IsTrue(typeof(Manager).IsAssignableFrom(managerType));
        Assert.IsTrue(typeof(Manager).IsAssignableFrom(dependsOn));
        ManagerAttributes attribs;
        if(instance.m_attributes.TryGetValue(managerType, out attribs))
        {
            return attribs.managerDependencyTypesLookup.Contains(dependsOn);
        }

        return false;
    }

    public static bool IsManagerAlwaysGlobal(System.Type managerType)
    {
        Assert.IsNotNull(managerType);
        Assert.IsTrue(typeof(Manager).IsAssignableFrom(managerType));
        ManagerAttributes attribs;
        if(instance.m_attributes.TryGetValue(managerType, out attribs))
        {
            return attribs.isAlwaysGlobal;
        }
        return false;
    }
}