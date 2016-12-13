using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using Type = System.Type;
using UnityEngine.Assertions;

#if UNITY_EDITOR
using System.Linq;
#endif

/// <summary>
/// Cache for ComponentDependencyAttribute so GetAttributes doesn't need to be called on each type 
/// at runtime (potential GC Alloc and performance spikes)
/// </summary>
public class ComponentDependencyCache : ResourceSingleton<ComponentDependencyCache>
, ISerializationCallbackReceiver
{
    [System.Serializable]
    public struct SerializedDependency
    {
        public string requiredTypeName;
        public string defaultTypeName;
    }

    [System.Serializable]
    public struct SerializedItem 
    {
        public string typeName;    
        public SerializedDependency[] dependencies;
    }  
    /// <summary>
    /// Serialized version of dependency table to be loaded at runtime.
    /// </summary>
    [SerializeField] List<SerializedItem> m_serializedItems = new List<SerializedItem>();

    public struct Dependency
    {
        public Type requiredType;
        public Type defaultType;
    }

    /// <summary>
    /// Dependencies table for all types using ComponentDepenencyAttribute
    /// </summary>
    Dictionary<Type, Dependency[]> m_dependencies = new Dictionary<Type, Dependency[]>();


    static Dependency[] GetDependencies(Type forType)
    {
        Dependency[] output = null;
        if(instance.m_dependencies.TryGetValue(forType, out output))
        {
            return output;
        }

        return new Dependency[0];
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
            if(gameObject.GetComponent(dep.requiredType)) continue;
            gameObject.AddComponent(dep.defaultType);
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
                if(typeof(Component).IsAssignableFrom(dep.requiredType))
                {
                    var component = gameObject.GetComponent(dep.requiredType);
                    if(!component) 
                    {
                        Debug.Log("ComponentDependencyAttribute: Creating default component type "+dep.defaultType.Name);
                        gameObject.AddComponent(dep.defaultType);                      
                    }
                }
            }
        }
    }
    #endif

    #if UNITY_EDITOR
    /*
    public class Builder : UnityEditor.AssetPostprocessor
    {
        static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) 
        {
            if(importedAssets
                .Concat(movedAssets)
                .Concat(deletedAssets)
                .Any(x=> x.EndsWith(".cs") || x.EndsWith(".js")))
            {
                ComponentDependencyCache.ProcessDependencies();
            }
        }
    }*/
    /*
    [UnityEditor.InitializeOnLoadMethod]
    static void InitializeOnLoad()
    {
        if (UnityEditor.BuildPipeline.isBuildingPlayer || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            ProcessDependencies();
        }
        else
        {
            UnityEditor.EditorApplication.delayCall += ProcessDependencies;
        }        
    }
    */
    /*
    [UnityEditor.Callbacks.PostProcessScene]
    static void PostProcessScene()
    {
        if (!Application.isPlaying && UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().buildIndex <= 0)
        {
            ProcessDependencies();                        
        }
    }*/

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void ProcessDependencies()
    { 
        ResourceSingletonBuilder.BuildResourceSingletonsIfDirty();

        var types = new string[] { ".cs", ".js" };

        var allScriptPaths = Directory.GetFiles("Assets", "*.*", SearchOption.AllDirectories)
            .Where(s => types.Any(x => s.EndsWith(x, System.StringComparison.CurrentCultureIgnoreCase)))
            .ToArray();

        for (int i = 0; i < allScriptPaths.Length; ++i)
        {
            UnityEditor.MonoScript script = UnityEditor.AssetDatabase.LoadAssetAtPath(allScriptPaths[i], typeof(UnityEditor.MonoScript)) as UnityEditor.MonoScript;
            if (!script || script.GetClass() == null) continue;
            if (!typeof(Component).IsAssignableFrom(script.GetClass())) continue;

            var type = script.GetClass();
            var attributes = (ComponentDependencyAttribute[])type.GetCustomAttributes(typeof(ComponentDependencyAttribute), true);
            if (attributes.Length == 0) continue;

            var dependencies = attributes
                .Where(x => x != null)
                .SelectMany(x => x.GetComponentDependencies())
                .ToArray();

            ProcessAll_SetDependencies(type, dependencies);
        }
    }

    static void ProcessAll_SetDependencies(Type type, Dependency[] dependencies)
    {  
        var items = instance.m_serializedItems;
        if(dependencies == null) 
        {
            dependencies = new Dependency[0];
        } 
           
        items.RemoveAll(x=>x.typeName==type.Name); 
        if(dependencies.Length>0)
        {
            var seralisedDeps = new List<SerializedDependency>();
            foreach(var dependency in dependencies)
            {
                Assert.IsNotNull(dependency.requiredType);
                Assert.IsNotNull(dependency.defaultType);
                seralisedDeps.Add(new SerializedDependency(){
                    requiredTypeName = dependency.requiredType.FullName,
                    defaultTypeName = dependency.defaultType.FullName,
                });
            }
            items.Add(new SerializedItem(){
                typeName = type.FullName,
                dependencies = seralisedDeps.ToArray()
            }); 
        }

        instance.hideFlags = HideFlags.NotEditable;
        UnityEditor.EditorUtility.SetDirty(instance);

        var so = new UnityEditor.SerializedObject(instance);       
        so.Update();

        UnityEditor.AssetDatabase.SaveAssets();
    }
    #endif

    #region ISerializationCallbackReceiver implementation
    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        m_dependencies.Clear();
        for(int i=0;i<m_serializedItems.Count;++i)
        {
            var item = m_serializedItems[i];
            if(string.IsNullOrEmpty(item.typeName)) continue;

            var forType = GetType(item.typeName);
            if(forType==null)
            {
                continue; 
            }

            List<Dependency> list = new List<Dependency>();
            for(int j=0;j<item.dependencies.Length;++j)
            {
                var dependency = new Dependency();
                var dep = item.dependencies[j];

                if(!string.IsNullOrEmpty(dep.requiredTypeName)) 
                {
                    dependency.requiredType = GetType(dep.requiredTypeName);
                }
                  
                if(dependency.requiredType==null)
                {
#if DEBUG_LOG
                    Debug.Log("ComponentDependencyCache: Skipping missing type "+dep.requiredTypeName);
#endif
                    continue;
                }

                if(!string.IsNullOrEmpty(dep.defaultTypeName))
                {
                    dependency.defaultType = GetType(dep.defaultTypeName);
                }

                if(dependency.defaultType==null)
                {
                    Debug.LogError("ComponentDependencyCache: Could not find type "+dep.defaultTypeName);
                    continue;
                }

                list.Add(dependency);
            } 

            m_dependencies[forType] = list.ToArray();
        }
    }    
#endregion
    static System.Type GetType(string name)
    { 
        System.Type type = null;
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            type = assemblies[i].GetType(name);
            if (type != null) break;
        }
        return type;
    }
}
