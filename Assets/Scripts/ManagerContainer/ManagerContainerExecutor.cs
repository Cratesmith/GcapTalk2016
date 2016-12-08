using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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

    void Awake()          { ManagerContainer.InitAllContainers();  m_initialized = true;}
    void Update()         { ManagerContainer.Execute(x=>x.OnUpdate()); }
    void FixedUpdate()    { ManagerContainer.Execute(x=>x.OnFixedUpdate()); }
    void LateUpdate()     { ManagerContainer.Execute(x=>x.OnLateUpdate()); }
}
