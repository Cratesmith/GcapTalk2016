using UnityEngine;
using System.Collections;

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
}


