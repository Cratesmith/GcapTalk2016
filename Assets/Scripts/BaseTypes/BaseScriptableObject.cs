using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Basetype for ScriptableObjects. 
/// This adds methods for accessing managers
/// </summary>
[ScriptDependencyAttribute(typeof(ManagerContainer))]
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
