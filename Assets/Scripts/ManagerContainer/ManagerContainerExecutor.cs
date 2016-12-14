using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[ScriptDependencyAttribute(typeof(ManagerContainer))]
[ScriptExecutionOrder(-16000)]
public class ManagerContainerExecutor : MonoBehaviour
{
    static ManagerContainerExecutor s_instance;
    bool m_initialized = false;

    public static bool Init()
    {
        if(s_instance==null)
        {
            var gameObject = new GameObject("ManagerContainerExecutor");
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            s_instance = gameObject.AddComponent<ManagerContainerExecutor>();
            DontDestroyOnLoad(gameObject);
            return false;
        }
        else 
        {
            return s_instance.m_initialized;
        }
    }

    void Awake()          
    { 
        ManagerContainer.InitAllContainers();
        m_initialized = true;
    }

    static readonly System.Action<Manager> callOnUpdate = (x)=>x.OnUpdate();
    void Update()         
    { 
        ManagerContainer.StartOfFrame(); 
        ManagerContainer.ExecuteOnAllManagers(callOnUpdate); 
    }
    
    static readonly System.Action<Manager> callFixedUpdate = (x)=>x.OnFixedUpdate();
    void FixedUpdate()    
    { 
        ManagerContainer.ExecuteOnAllManagers(callFixedUpdate); 
    }

    static readonly System.Action<Manager> callLateUpdate = (x)=>x.OnLateUpdate();
    void LateUpdate()     
    {
        ManagerContainer.ExecuteOnAllManagers(callLateUpdate); 
    }
}
