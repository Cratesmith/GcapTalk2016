using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class Manager : ScriptableObject
{
    // called by container
    public virtual void OnAwake()       { }
    public virtual void OnStart()       { }
    public virtual void OnUpdate()      { }
    public virtual void OnFixedUpdate() { }
    public virtual void OnLateUpdate()  { }

    // OnDestroy called by unity
    public virtual void OnDestroy()     { }

    public ManagerContainer container   { get; set; }
    public bool enabled { get; set; }

    public static System.Type[] GetDependencies(System.Type type)
    {        
        return ManagerAttributeCache.GetManagerDependencies(type);
    }

    public System.Type[] GetDependencies()
    {
        return ManagerAttributeCache.GetManagerDependencies(GetType());
    }

    public T GetManager<T>() where T:Manager
    {
        if(!ManagerAttributeCache.DoesManagerDependOn(GetType(), typeof(T)))
        {
            Debug.LogError(string.Format("{0} Doesn't depend on {1}, it must have the attribute [ManagerDependency(typeof({1}))]", GetType().Name, typeof(T).Name));
            return null;
        }

        var result = container.GetManager<T>();
        if (result == null)
        {
            Debug.LogError(string.Format("{0} wasn't able to acquire manager of type {1} (a registered dependency).", GetType().Name, typeof(T).Name));
            return null;
        }

        return result;
    }
}
