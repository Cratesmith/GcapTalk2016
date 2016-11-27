using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class BaseMonoBehaviour : MonoBehaviour
{
    public T GetManager<T>() where T:Manager
    {
        return ManagerContainer.GetManager<T>(gameObject.scene);
    }

    public T GetGlobalManager<T>() where T:Manager
    {
        return ManagerContainer.GetGlobalManager<T>();
    }

    protected virtual void Awake()
    {
        ComponentDependencyCache.CreateDependencies_Runtime(this);
    }

    #if UNITY_EDITOR
    protected virtual void Reset()
    { 
        ComponentDependencyCache.CreateDependencies_Editor(this);
    }
    #else
    protected virtual void Reset() {}
    #endif
}


