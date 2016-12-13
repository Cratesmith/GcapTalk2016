using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using Type = System.Type;
using UnityEngine.Assertions;


#if UNITY_EDITOR
using System.Linq;
#endif

/// <summary>
/// This component depends on another component
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
public class ComponentDependencyAttribute : ScriptDependencyAttribute
{   
    public Type defaultComponentType        {get;private set;}
    public bool m_executesAfterDependencies {get;private set;}

    /// <summary>
    /// Declare a component dependency
    /// </summary>
    /// <param name="requiredAndDefaultType">A non abstract component type that this class will require</param>
    /// <param name="executesAfterDependency">If set to <c>true</c> this class executes after the dependency class.</param>
    public ComponentDependencyAttribute(Type requiredAndDefaultType, bool executesAfterDependency=true) : base(requiredAndDefaultType)
    {
        Init(requiredAndDefaultType, requiredAndDefaultType, executesAfterDependency);    
    }

    /// <summary>
    /// Declare a component dependency
    /// </summary>
    /// <param name="requiredType">Required type.</param>
    /// <param name="defaultType">Default type.</param>
    /// <param name="executesAfterDependency">If set to <c>true</c> executes after dependency.</param>
    public ComponentDependencyAttribute(Type requiredType, Type defaultType, bool executesAfterDependency=true) : base(requiredType)
    {
        Init(requiredType, defaultType, executesAfterDependency);
    }

    void Init(Type requiredType, Type defaultType, bool executesAfterDependency)
    {
        Assert.IsTrue(requiredType != null && defaultType != null, "ComponentDependency: types cannot be NULL");
        Assert.IsTrue(!defaultType.IsAbstract, "ComponentDependency: default type " + defaultType.Name + " cannot be abstract");
        Assert.IsTrue(typeof(Component).IsAssignableFrom(defaultType), "ComponentDependency: default type " + defaultType.Name + " is not a Component");
        Assert.IsTrue(requiredType.IsAssignableFrom(defaultType), "ComponentDependency default type must be same as or assignable to required type");
        defaultComponentType = defaultType;
        m_executesAfterDependencies = executesAfterDependency;
    }

    #if UNITY_EDITOR
    /// <summary>
    /// Retreive the dependencies for this type
    /// </summary>
    /// <returns>The component dependencies.</returns>
    public ComponentDependencyCache.Dependency[] GetComponentDependencies()
    {
        var dependency = new ComponentDependencyCache.Dependency {
            requiredType = scriptDependencyType,
            defaultType = defaultComponentType,
        };

        return new ComponentDependencyCache.Dependency[] { dependency };
    }

    public override Type[] GetScriptDependencies()
    {
        return m_executesAfterDependencies?base.GetScriptDependencies():new Type[0];
    }
    #endif
}
