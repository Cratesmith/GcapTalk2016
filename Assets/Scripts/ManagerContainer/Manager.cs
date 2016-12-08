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
        List<System.Type> dependencies = new List<System.Type>();
        var interfaces = type.GetInterfaces();
        for (int i = 0; i < interfaces.Length; i++)
        {
            var current = interfaces[i];
            if (current == null || !current.IsGenericType)
                continue;

            if(current.GetGenericTypeDefinition() != typeof(IManagerDependency<>))
                continue;

            dependencies.Add(current.GetGenericArguments()[0]);
        }
        return dependencies.ToArray();
    }

    public System.Type[] GetDependencies()
    {
        return Manager.GetDependencies(GetType());
    }

    public T GetManager<T>() where T:Manager
    {
        var source = this as IManagerDependency<T>;
        if (source == null)
        {
            Debug.LogError(string.Format("{0} doesn't implement IManagerDependency<{1}>", GetType().Name, typeof(T).Name));
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
