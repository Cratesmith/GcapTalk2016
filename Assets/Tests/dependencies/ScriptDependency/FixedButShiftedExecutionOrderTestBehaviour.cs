using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;

#if UNITY_EDITOR
using System.Linq;
#endif

[ScriptExecutionOrderAttribute(100)]
[ScriptDependencyAttribute(typeof(FixedExecutionOrderTestBehaviour))]
public class FixedButShiftedExecutionOrderTestBehaviour : ScriptDependencyTestBehaviour
{
}
