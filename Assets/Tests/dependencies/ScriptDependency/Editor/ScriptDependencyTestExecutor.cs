using UnityEngine;
using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;

#if UNITY_EDITOR
using System.Linq;
#endif


[TestFixture]
public class ScriptDependencyTestExecutor 
{
    public class ScriptData
    {
        public UnityEditor.MonoScript   script;
        public int?                     fixedOrderValue;
        public List<ScriptData>         dependsOn = new List<ScriptData>();
        public List<ScriptData>         dependedOnBy = new List<ScriptData>();
    }

    Dictionary<System.Type, UnityEditor.MonoScript> typeLookup = new Dictionary<System.Type, UnityEditor.MonoScript>();
    Dictionary<UnityEditor.MonoScript, ScriptData> scriptDataTable = new Dictionary<UnityEditor.MonoScript, ScriptData>();
     
    ScriptData Init_Script(UnityEditor.MonoScript script)
    {
        if (script == null)
            throw new System.ArgumentNullException("Init_Script: script cannot be null");
        
        var scriptClass = script.GetClass();
        if (scriptClass == null)
            throw new System.ArgumentNullException("Init_Script: must be a monoscript with a valid class");

        ScriptData scriptData = null;
        if(scriptDataTable.TryGetValue(script, out scriptData))
        {
            return scriptData;
        }
        else 
        {            
            scriptData = scriptDataTable[script] = new ScriptData();
            scriptData.script = script;
        }


        var fixedOrderAttribute = scriptClass.GetCustomAttributes(typeof(ScriptExecutionOrderAttribute), true).Cast<ScriptExecutionOrderAttribute>().FirstOrDefault();
        if (fixedOrderAttribute!=null)
        {
            scriptData.fixedOrderValue = fixedOrderAttribute.order;
        }
        var dependsOnAttributes = scriptClass.GetCustomAttributes(typeof(ScriptDependencyAttribute), true).Cast<ScriptDependencyAttribute>().ToArray();
        foreach (var i in dependsOnAttributes)
        {
            var dependsOnTypes = i.GetScriptDependencies()
                .Where(x=>typeLookup.ContainsKey(x))
                .Select(x=>typeLookup[x])
                .ToArray();

            foreach(var j in dependsOnTypes)
            {
                var dependsOnSD = Init_Script(j);
                dependsOnSD.dependedOnBy.Add(scriptData);
                scriptData.dependsOn.Add(dependsOnSD);
            }
        }

        return scriptData;
    }

    [SetUp]
    public void Init()
    {
        var allScripts = UnityEditor.MonoImporter.GetAllRuntimeMonoScripts();
        foreach(var script in allScripts)
        {
            if(script==null) continue;
            var scriptClass = script.GetClass();
            if(scriptClass==null) continue;
            typeLookup[scriptClass] = script;
        }

        foreach(var script in allScripts)
        {
            if(script==null) continue;
            var scriptClass = script.GetClass();
            if(scriptClass==null) continue;
            Init_Script(script);            
        }
    }

    [TearDown]
    public void Close()
    {
        scriptDataTable = new Dictionary<UnityEditor.MonoScript, ScriptData>();
        typeLookup = new Dictionary<System.Type, UnityEditor.MonoScript>();
    }

    [Test]
    public void AllFixedAtSetExecutionOrderUnlessShifted()
    {
        foreach(var i in scriptDataTable.Values)
        {
            if(!i.fixedOrderValue.HasValue) continue;
            if(i.dependsOn.Count > 0 ) continue; // for now ignore any that MIGHT be shifted.
            Assert.AreEqual(UnityEditor.MonoImporter.GetExecutionOrder(i.script), i.fixedOrderValue.Value);
        }
    }        

    [Test]
    public void AllScriptsAfterDependencies()
    {
        foreach(var scriptData in scriptDataTable.Values)
        {
            var scriptEO = UnityEditor.MonoImporter.GetExecutionOrder(scriptData.script);
            foreach(var dependsOn in scriptData.dependsOn)
            {
                var depEO = UnityEditor.MonoImporter.GetExecutionOrder(dependsOn.script);
                Assert.Greater(scriptEO, depEO, scriptData.script.name+" order("+scriptEO+") must be greater than "+dependsOn.script.name+" order("+depEO+")");
            }
        }
        /*
        var allScripts = UnityEditor.MonoImporter.GetAllRuntimeMonoScripts();

        foreach(var script in allScripts)
        {
            var scriptClass = script.GetClass();
            if(!typeof(MonoBehaviour).IsAssignableFrom(scriptClass) 
                && !typeof(ScriptableObject).IsAssignableFrom(scriptClass))
            {
                continue;
            }

            var scriptDependencyAttributes = (ScriptDependencyAttribute[])scriptClass.GetCustomAttributes(typeof(ScriptDependencyAttribute), true);

            foreach(var i in scriptDependencyAttributes)
            {
                foreach(var j in i.GetScriptDependencies())
                {
                    var depScript = UnityEditor.MonoImporter.GetAllRuntimeMonoScripts().FirstOrDefault(x=>x.GetType()==j);
                    if(depScript==null) continue;
                    Assert.IsTrue(UnityEditor.MonoImporter.GetExecutionOrder(script) > UnityEditor.MonoImporter.GetExecutionOrder(depScript));
                }
            }
        }
        */
    }
}
