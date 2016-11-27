using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using Type = System.Type;

#if UNITY_EDITOR
using System.Linq;
#endif

public class ComponentDependencyCache : ResourceSingleton<ComponentDependencyCache>
, ISerializationCallbackReceiver
{
    [System.Serializable]
    public struct Item 
    {
        public string   forType;
        public string[] dependsOn;
        #if UNITY_EDITOR
        public void WriteToProperty(UnityEditor.SerializedProperty property)
        {
            var forTypeProp = property.FindPropertyRelative("forType");
            forTypeProp.stringValue = forType;

            var dependsOnProp = property.FindPropertyRelative("dependsOn");
            dependsOnProp.arraySize = dependsOn!=null ? dependsOn.Length:0;
            for(int i=0;i<dependsOnProp.arraySize;++i)
            {
                dependsOnProp.GetArrayElementAtIndex(i).stringValue = dependsOn[i];
            }
        }
        #endif
    }
    [SerializeField] List<Item> m_items = new List<Item>();

    Dictionary<Type, Type[]> m_dependencies = new Dictionary<Type, Type[]>();

    static Type[] GetDependencies(Type forType)
    {
        Type[] output = null;
        if(instance.m_dependencies.TryGetValue(forType, out output))
        {
            return output;
        }

        return new Type[0];
    }

    public static void CreateDependencies_Runtime(Component forComponent)
    {
        if(forComponent==null) return;
        var gameObject = forComponent.gameObject;
        var type = forComponent.GetType();

        var dependencies = ComponentDependencyCache.GetDependencies(type);
        for(int i=0;i<dependencies.Length;++i)
        {
            var dep = dependencies[i];
            if(gameObject.GetComponent(dep)) continue;
            gameObject.AddComponent(dep);
        }        
    }

    #if UNITY_EDITOR
    public static void CreateDependencies_Editor(Component forComponent)
    {
        if(forComponent==null) return;
        var gameObject = forComponent.gameObject;

        var type = forComponent.GetType();
        var depenencyAttributes = (ComponentDependencyAttribute[])type.GetCustomAttributes(typeof(ComponentDependencyAttribute), true);

        for(int i=0; i<depenencyAttributes.Length; ++i)
        {
            var attrib = depenencyAttributes[i];
            var deps = attrib.GetComponentDependencies();
            for(int j=0; j<deps.Length; ++j)
            {
                var dep = deps[j];
                if(typeof(Component).IsAssignableFrom(dep))
                {
                    var component = gameObject.GetComponent(dep);
                    if(!component) 
                    {
                        Debug.Log("ComponentDependencyAttribute: Creating required component "+dep.Name);
                        gameObject.AddComponent(dep);                      
                    }
                }
            }
        }
    }
    #endif

    #if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    static void ProcessAll()
    {
        var types = new string[] {".cs", ".js"};

        var allScriptPaths = Directory.GetFiles("Assets", "*.*", SearchOption.AllDirectories)
            .Where(s => types.Any(x=>s.EndsWith(x, System.StringComparison.CurrentCultureIgnoreCase)))
            .ToArray();

        for(int i=0;i<allScriptPaths.Length; ++i)
        {
            UnityEditor.MonoScript script = UnityEditor.AssetDatabase.LoadAssetAtPath(allScriptPaths[i], typeof(UnityEditor.MonoScript)) as UnityEditor.MonoScript;
            if(!script || script.GetClass()==null) continue;
            if(typeof(Component).IsAssignableFrom(script.GetClass())) continue;

            var type = script.GetClass();
            var dependencies = type.GetCustomAttributes(typeof(ComponentDependencyAttribute),true)
                .Cast<ComponentDependencyAttribute>()
                .SelectMany(x=>x.GetScriptDependencies())
                .ToArray();

            ProcessAll_SetDependencies(type, dependencies);
        }
    }

    static void ProcessAll_SetDependencies(Type forType, Type[] dependsOn)
    {
        var so = new UnityEditor.SerializedObject(instance);

        var items = instance.m_items;

        if(dependsOn == null) 
        {
            dependsOn = new Type[0];
        }

        items.RemoveAll(x=>x.forType==forType.Name);
        items.Add(new Item(){
            forType = forType.Name,
            dependsOn = dependsOn.Select(x=>x.Name).ToArray()
        });
            
        var itemsProp = so.FindProperty("m_items");
        itemsProp.arraySize = items.Count;
        for(int i=0; i<items.Count; ++i)
        {
            items[i].WriteToProperty(itemsProp.GetArrayElementAtIndex(i));
        }
            
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    #endif

    #region ISerializationCallbackReceiver implementation
    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        m_dependencies.Clear();
        for(int i=0;i<m_items.Count;++i)
        {
            var item = m_items[i];
            var forType = GetType().Assembly.GetType(item.forType);
            if(forType==null)
            {
                continue;
            }

            List<Type> list = new List<Type>();
            for(int j=0;j<item.dependsOn.Length;++j)
            {
                var depString = item.dependsOn[j];
                var depType = GetType().Assembly.GetType(depString);
                if(depType==null)
                {
                    Debug.LogError("ComponentDependencyCache: Could not find type "+depString);
                    continue;
                }

                list.Add(depType);
            }

            m_dependencies[forType] = list.ToArray();
        }
    }
    #endregion
}
