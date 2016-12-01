using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using Type = System.Type;
using UnityEngine.Assertions;


#if UNITY_EDITOR
using System.Linq;
#endif


public class ComponentDependencyAttribute : ScriptDependencyAttribute
{   
    #if UNITY_EDITOR
    public readonly Type defaultComponentType;
    public readonly bool m_executesAfterDependencies;

    public ComponentDependencyAttribute(Type requiredAndDefaultType, bool executesAfterDependency=true) : base(requiredAndDefaultType)
    {
        Assert.IsNotNull(requiredAndDefaultType, "ComponentDependency: types cannot be NULL");
        Assert.IsTrue(!requiredAndDefaultType.IsAbstract, "ComponentDependency: default type cannot be abstract");
        defaultComponentType = requiredAndDefaultType;
        m_executesAfterDependencies = executesAfterDependency;       
    }
          
    public ComponentDependencyAttribute(Type requiredType, Type defaultType, bool executesAfterDependency=true) : base(requiredType)
    {
        Assert.IsTrue(requiredType!=null && defaultType!=null, "ComponentDependency: types cannot be NULL");
        Assert.IsTrue(!defaultType.IsAbstract, "ComponentDependency default type cannot be abstract");
        Assert.IsTrue(requiredType.IsAssignableFrom(defaultType), "ComponentDependency default type must be same as or assignable to required type");
        defaultComponentType = defaultType;
        m_executesAfterDependencies = executesAfterDependency;       
    }

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
