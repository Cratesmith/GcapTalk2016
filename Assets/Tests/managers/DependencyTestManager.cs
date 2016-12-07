using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;

[ManagerDependencyAttribute(typeof(TestManager))]
public class DependencyTestManager : TestManager 
{
    public TestManager dependency;

    public override void OnAwake()
    {
        base.OnAwake();
        dependency = GetManager<TestManager>();
        Assert.IsNotNull(dependency);
        Assert.IsTrue(dependency.awakes==awakes);
        Assert.IsTrue(dependency.starts==starts);
    }

    public override void OnStart()
    {
        Assert.IsTrue(dependency.starts>starts);
        base.OnStart();
        Assert.IsTrue(dependency.starts==starts);
    }

    public override void OnUpdate()
    {
        Assert.IsTrue(dependency.updates>updates);
        base.OnUpdate();
        Assert.IsTrue(dependency.updates==updates);
    }

    public override void OnFixedUpdate()
    {
        Assert.IsTrue(dependency.fixedUpdates>fixedUpdates);
        base.OnFixedUpdate();
        Assert.IsTrue(dependency.fixedUpdates==fixedUpdates);
    }

    public override void OnLateUpdate()
    {
        Assert.IsTrue(dependency.lateUpdates>lateUpdates);
        base.OnLateUpdate();
        Assert.IsTrue(dependency.lateUpdates==lateUpdates);
    }
}
