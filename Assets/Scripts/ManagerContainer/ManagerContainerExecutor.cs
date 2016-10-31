using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class ManagerContainerExecutor : MonoBehaviour
{
    static ManagerContainerExecutor s_instance;

    static void Init()
    {
        if(s_instance==null)
        {
            var gameObject = new GameObject("ManagerContainerExecutor");
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            s_instance = gameObject.AddComponent<ManagerContainerExecutor>();
            DontDestroyOnLoad(gameObject);
        }
    }

    protected void Update()         { ManagerContainer.Execute(x=>x.OnUpdate()); }
    protected void FixedUpdate()    { ManagerContainer.Execute(x=>x.OnFixedUpdate()); }
    protected void LateUpdate()     { ManagerContainer.Execute(x=>x.OnLateUpdate()); }
}
