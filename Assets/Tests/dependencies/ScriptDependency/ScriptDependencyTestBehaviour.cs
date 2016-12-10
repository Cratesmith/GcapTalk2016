using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;

#if UNITY_EDITOR
using System.Linq;
#endif
/*
#if UNITY_EDITOR
public class ScriptDependencyTestBehaviourProcessor : UnityEditor.AssetPostprocessor
{
    static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) 
    {
        var importedScripts = importedAssets.Where(x=> x.EndsWith(".cs") || x.EndsWith(".js")).ToArray();
        foreach(var i in importedScripts)
        {
            var script = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>(i);
            if(script==null 
                || !typeof(ScriptDependencyTestBehaviour).IsAssignableFrom(script.GetClass()))
            {
                continue;
            }
                
            var so = new UnityEditor.SerializedObject(script);
            var orderProp = so.FindProperty("order");
            orderProp.intValue = UnityEditor.MonoImporter.GetExecutionOrder(script);
        }
    }
}
#endif
*/
public class ScriptDependencyTestBehaviour : BaseMonoBehaviour 
{
    public int awakes       {get;set;}
    public int starts       {get;set;}
    public int updates      {get;set;}
    public int fixedUpdates {get;set;}
    public int lateUpdates  {get;set;}

    public void LogMethod(string methodName)
    {
        Debug.Log(name+":"+GetType().Name+": "+methodName+" "+awakes+"a, "+starts+"s, "+updates+"u, "+fixedUpdates+"f, "+lateUpdates+"l");
    }

    protected override void Awake()
    {
        base.Awake();
        ++awakes;

        LogMethod("OnAwake");
        Assert.AreEqual(awakes,1, "Only awake once");
        Assert.AreEqual(starts,0, "Awake before start");
        Assert.IsFalse(updates>0, "No update before start/awake");
        Assert.IsFalse(fixedUpdates>0, "No fixedUpdate before start/awake");
        Assert.IsFalse(lateUpdates>0, "No lateUpdates before start/awake");

        #if UNITY_EDITOR 
        var scriptDependencyAttributes = (ScriptDependencyAttribute[])GetType().GetCustomAttributes(typeof(ScriptDependencyAttribute), true);
        var script = UnityEditor.MonoScript.FromMonoBehaviour(this);

        foreach(var i in scriptDependencyAttributes)
        {
            foreach(var j in i.GetScriptDependencies())
            {
                var depScript = UnityEditor.MonoImporter.GetAllRuntimeMonoScripts().FirstOrDefault(x=>x.GetType()==j);
                if(depScript==null) continue;

                Assert.IsTrue(UnityEditor.MonoImporter.GetExecutionOrder(script) > UnityEditor.MonoImporter.GetExecutionOrder(depScript));
            }
        }
        #endif
    }

    void Start()
    {
        ++starts;

        LogMethod("OnStart");
        Assert.AreEqual(awakes,1, "Only awake once");
        Assert.AreEqual(starts,1, "Only start once");
        Assert.IsFalse(updates>0, "No update before start/awake");
        Assert.IsFalse(fixedUpdates>0, "No fixedUpdate before start/awake");
        Assert.IsFalse(lateUpdates>0, "No lateUpdates before start/awake");
    }

    void Update()
    {
        ++updates;

        LogMethod("OnUpdate");
        Assert.AreEqual(awakes,1, "Only awake once");
        Assert.AreEqual(starts,1, "Only start once");
        Assert.IsTrue(updates>0, "Must have updated");
        Assert.IsFalse(lateUpdates>=updates, "Cannot lateupdate more than updates");
    }

    void FixedUpdate()
    {
        ++fixedUpdates;

        LogMethod("OnFixedUpdate");
        Assert.AreEqual(awakes,1, "Only awake once");
        Assert.AreEqual(starts,1, "Only start once");
        Assert.IsTrue(fixedUpdates>0, "Must have fixedupdated");
    }

    void LateUpdate()
    {
        ++lateUpdates;

        LogMethod("OnLateUpdate");
        Assert.AreEqual(awakes,1, "Only awake once");
        Assert.AreEqual(starts,1, "Only start once");
        Assert.IsTrue(updates>0, "Must have updated");
        Assert.AreEqual(lateUpdates, updates, "Late updates must match updates");
    }
}
