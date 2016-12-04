using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// Manager container.
/// </summary>
[ScriptExecutionOrder(-16000)] // execute as early as possible. Managers are supposed to run before objects in the scene
public partial class ManagerContainer : MonoBehaviour
{
    [SerializeField] Manager[]  m_managerPrefabs = new Manager[0];
    [SerializeField] bool       m_createAsGlobalContainer = false;

    public bool isGlobalContainer { get { return s_globalContainer == this; } }

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

        for(int i=0;i<m_managerInstances.Count; ++i) {
            m_managerInstances[i].OnStart();
        }        
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

    void ExecuteManagers(System.Action<Manager> action)
    {
        for(int i=0;i<m_managerInstances.Count; ++i) {
            action.Invoke(m_managerInstances[i]);
        }
    }

    public static void InitAllContainers()
    {
        if(s_globalContainer) 
        {
            s_globalContainer.InitContainer();
        }

        var e = s_managerContainers.GetEnumerator();
        while(e.MoveNext()) 
        {
            e.Current.Value.InitContainer();
        }
    }
        
    public static void Execute(System.Action<Manager> action)
    {
        if(s_globalContainer) 
        {
            s_globalContainer.ExecuteManagers(action);
        }

        var e = s_managerContainers.GetEnumerator();
        while(e.MoveNext()) 
        {
            e.Current.Value.ExecuteManagers(action);
        }
    }
}
