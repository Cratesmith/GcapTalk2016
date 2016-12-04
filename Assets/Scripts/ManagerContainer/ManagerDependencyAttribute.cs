using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;

#if UNITY_EDITOR
using System.IO;
using System.Linq;
#endif

public class ManagerDependencyAttribute : System.Attribute
{
    #if UNITY_EDITOR
    public readonly System.Type managerDependency;

    public ManagerDependencyAttribute(System.Type dependsOnManagerType)
    {
        Assert.IsNotNull(dependsOnManagerType);
        Assert.IsTrue(typeof(Manager).IsAssignableFrom(dependsOnManagerType));
        managerDependency = dependsOnManagerType;
    }

    public System.Type[] GetManagerDependencies() 
    {
        return new System.Type[] { managerDependency };
    }
    #endif
}
