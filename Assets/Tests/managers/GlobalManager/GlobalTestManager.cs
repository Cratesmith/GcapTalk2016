using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;

[ManagerAlwaysGlobal]
public class GlobalTestManager : TestManager 
{
    public override void OnAwake()
    {
        base.OnAwake();
        Assert.IsTrue(container.isGlobalContainer);
    }
}