using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;

public class GlobalManagerTestExecutor : BaseMonoBehaviour
{
    protected override void Awake()
    {
        base.Awake();
        StartCoroutine(TestCoroutine());
    }

    IEnumerator TestCoroutine()
    {
        Debug.Log("GlobalManagerTestExecutor: Starting test");
        var manager = GetManager<GlobalTestManager>();
        Assert.IsNotNull(manager);
        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(manager.awakes==1);
        Assert.IsTrue(manager.starts==1);
        Assert.IsTrue(manager.updates>1);
        Assert.IsTrue(manager.fixedUpdates>1);
        Assert.IsTrue(manager.lateUpdates>1);

        IntegrationTest.Pass();

        Destroy(manager.container); // destroy the global container so it doesn't continue into other tests.
    }
}