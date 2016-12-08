using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;

public class TestManager : Manager 
{
    public int awakes = 0;
    public int starts = 0;
    public int updates = 0;
    public int fixedUpdates = 0;
    public int lateUpdates = 0;

    public void LogMethod(string methodName)
    {
        Debug.Log(name+": "+methodName+" "+awakes+"a, "+starts+"s, "+updates+"u, "+fixedUpdates+"f, "+lateUpdates+"l");
    }

    public override void OnAwake()
    {
        base.OnAwake();
        ++awakes;

        LogMethod("OnAwake");
        Assert.AreEqual(awakes,1, "Only awake once");
        Assert.AreEqual(starts,0, "Awake before start");
        Assert.IsFalse(updates>0, "No update before start/awake");
        Assert.IsFalse(fixedUpdates>0, "No fixedUpdate before start/awake");
        Assert.IsFalse(lateUpdates>0, "No lateUpdates before start/awake");
    }

    public override void OnStart()
    {
        base.OnStart();
        ++starts;

//        LogMethod("OnStart");
        Assert.AreEqual(awakes,1, "Only awake once");
        Assert.AreEqual(starts,1, "Only start once");
        Assert.IsFalse(updates>0, "No update before start/awake");
        Assert.IsFalse(fixedUpdates>0, "No fixedUpdate before start/awake");
        Assert.IsFalse(lateUpdates>0, "No lateUpdates before start/awake");
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        ++updates;

//        LogMethod("OnUpdate");
        Assert.AreEqual(awakes,1, "Only awake once");
        Assert.AreEqual(starts,1, "Only start once");
        Assert.IsTrue(updates>0, "Must have updated");
        Assert.IsFalse(lateUpdates>=updates, "Cannot lateupdate more than updates");
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        ++fixedUpdates;

//        LogMethod("OnFixedUpdate");
        Assert.AreEqual(awakes,1, "Only awake once");
        Assert.AreEqual(starts,1, "Only start once");
        Assert.IsTrue(fixedUpdates>0, "Must have fixedupdated");
    }

    public override void OnLateUpdate()
    {
        base.OnLateUpdate();
        ++lateUpdates;

//        LogMethod("OnLateUpdate");
        Assert.AreEqual(awakes,1, "Only awake once");
        Assert.AreEqual(starts,1, "Only start once");
        Assert.IsTrue(updates>0, "Must have updated");
        Assert.AreEqual(lateUpdates, updates, "Late updates must match updates");
    }
}
