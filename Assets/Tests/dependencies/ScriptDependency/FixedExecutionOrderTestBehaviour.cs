using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;

#if UNITY_EDITOR
using System.Linq;
#endif

[ScriptExecutionOrderAttribute(101)]
public class FixedExecutionOrderTestBehaviour : ScriptDependencyTestBehaviour
{
}
