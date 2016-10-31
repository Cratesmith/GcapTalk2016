using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public abstract class BaseScriptableObject : ScriptableObject
{
    public T GetManager<T>(Scene scene) where T:Manager
    {
        return ManagerContainer.GetManager<T>(scene);
    }

    public T GetGlobalManager<T>() where T:Manager
    {
        return ManagerContainer.GetGlobalManager<T>();
    }
}
