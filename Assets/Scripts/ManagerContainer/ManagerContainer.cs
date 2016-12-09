using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
#if UNITY_5_5_OR_NEWER
using UnityEngine.EventSystems;
using UnityEngine.Profiling;
#endif

/// <summary>
/// Manager container.
/// </summary>
public partial class ManagerContainer : MonoBehaviour
{
    [SerializeField] Manager[]  m_managerPrefabs = new Manager[0];
    [SerializeField] bool       m_createAsGlobalContainer = false;

    public bool isGlobalContainer { get { return s_globalContainer == this; } }

    List<Manager>                       m_managersToStart   = new List<Manager>();
    List<Manager>                       m_managersToExecute = new List<Manager>();
    List<Manager>                       m_managerInstances  = new List<Manager>();
    Dictionary<System.Type, Manager>    m_managerLookup     = new Dictionary<System.Type, Manager>();

    public List<Manager>                managerInstances  { get {return m_managerInstances; } }
      
    protected void Awake() 
    {
        if(m_createAsGlobalContainer) {
            Debug.Assert(s_globalContainer==null, "There must be only one global container at a time");
            s_globalContainer = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Debug.Assert(!s_managerContainers.ContainsKey(gameObject.scene), "There must be only one scene container per scene");
            s_managerContainers[gameObject.scene] = this;
        }

        if(ManagerContainerExecutor.Init())
        {
            InitContainer();
        }
    }

    void InitContainer()
    {
        var sortedManagers = GetSortedManagers();
        for(int i=0;i<sortedManagers.Count; ++i) {
            if(sortedManagers[i]==null) continue;    
            if(!isGlobalContainer && ManagerAttributeCache.IsManagerAlwaysGlobal(sortedManagers[i].GetType()))
            {
                    Debug.LogError("Cannot add [ManagerAlwaysGlobal] manager prefab \""
                    +sortedManagers[i].name
                    +"\"("+sortedManagers[i].GetType().Name
                    +") to non-global container!"
                    +name
                    +"."); 
                continue;
            }
            var manager = Instantiate(sortedManagers[i]);
            AddNewManager(manager);
        }   

        StartNewManagers();    
    }

    void StartNewManagers()
    {
        Profiler.BeginSample("ManagerContainer.StartNewManagers");
        for (int i = 0; i < m_managersToStart.Count; ++i)
        {
            var manager = m_managersToStart[i];
            if (manager.enabled)
            {
                manager.OnStart();
                m_managersToExecute.Add(manager);
            }
        }

        if(m_managersToStart.Count > 0)
        {
            m_managersToStart.RemoveAll(x => x.enabled == true);
        }
        Profiler.EndSample();
    }

    Manager AutoconstructManager(System.Type managerType)
    {
        var autoBuiltManager = ScriptableObject.CreateInstance(managerType) as Manager;
        autoBuiltManager.name = managerType.Name + " (autoconstructed)";
        AddNewManager(autoBuiltManager);
        return autoBuiltManager;
    }

    void AddNewManager(Manager manager)
    {
        var deps = manager.GetDependencies();
        for(int i=0;i<deps.Length;++i)
        {
            if(m_managerLookup.ContainsKey(deps[i]))   
            {
                continue;
            }   

            if(s_globalContainer!=this && s_globalContainer!=null && s_globalContainer.m_managerLookup.ContainsKey(deps[i]))   
            {
                continue;
            }       

            AutoconstructManager(deps[i]);
        } 

        m_managerLookup.Add(manager.GetType(), manager);
        m_managerInstances.Add(manager);
        m_managersToStart.Add(manager);

        manager.container = this;
        manager.enabled = true;
        manager.OnAwake();
    }

    protected void OnDestroy() {
        for(int i=0;i<m_managerInstances.Count; ++i) {
            Destroy(m_managerInstances[i]);
        }
        m_managerInstances.Clear();
    }      

    void ExecuteOnManagers(System.Action<Manager> action)
    {
        Profiler.BeginSample("ManagerContainer.ExecuteOnManagers");
        for(int i=0;i<m_managersToExecute.Count; ++i) {
            var manager = m_managersToExecute[i];
            if(!manager.enabled) continue;                
            action.Invoke(manager);
        }
        Profiler.EndSample();
    }

    public static void InitAllContainers()
    {
        ExecuteOnAllContainers(container=>container.InitContainer());       
    }
        
    public static void StartOfFrame()
    {
        Profiler.BeginSample("ManagerContainer.StartOfFrame");
        ExecuteOnAllContainers(container=>container.StartNewManagers());
        Profiler.EndSample();
    }

    static void ExecuteOnAllContainers(System.Action<ManagerContainer> action)
    {
        Profiler.BeginSample("ManagerContainer.ExecuteOnAllContainers");
        if(s_globalContainer) 
        {
            action(s_globalContainer);
        }

        var e = s_managerContainers.GetEnumerator();
        while(e.MoveNext()) 
        {
            if(e.Current.Value.isGlobalContainer) continue;
            action(e.Current.Value);
        }
        Profiler.EndSample();
    }    

    public static void ExecuteOnAllManagers(System.Action<Manager> action)
    {
        Profiler.BeginSample("ManagerContainer.ExecuteOnAllManagers");
        if(s_globalContainer) 
        {
            s_globalContainer.ExecuteOnManagers(action);
        }

        var e = s_managerContainers.GetEnumerator();
        while(e.MoveNext()) 
        {
            if(e.Current.Value.isGlobalContainer) continue;
            e.Current.Value.ExecuteOnManagers(action);
        }
        Profiler.EndSample();
    }
}
