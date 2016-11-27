using UnityEngine;
using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Type = System.Type;


public class ComponentDependencyAttribute : ScriptDependencyAttribute
{    
    bool m_scriptOrderDependencyOrder;

    public ComponentDependencyAttribute(Type type, bool scriptOrderDependencyOrder=true) : base(type)
    {
        m_scriptOrderDependencyOrder = scriptOrderDependencyOrder;       
    }

    public Type[] GetComponentDependencies()
    {
        return base.GetScriptDependencies();
    }

    public override Type[] GetScriptDependencies()
    {
        return m_scriptOrderDependencyOrder?base.GetScriptDependencies():new Type[0];
    }
}
