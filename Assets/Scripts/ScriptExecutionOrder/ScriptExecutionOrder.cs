using UnityEngine;
using System.Collections;
using System.IO;
using System.Linq;

[System.AttributeUsage(System.AttributeTargets.All)]
public class ScriptExecutionOrderAttribute : System.Attribute 
{
	public readonly int order;
	public ScriptExecutionOrderAttribute(int order)
	{
		this.order = order;
	}
}

#if UNITY_EDITOR 
public class ScriptExecutionOrder : UnityEditor.AssetPostprocessor 
{
	static void OnPostprocessAllAssets(
		string[] importedAssets,
		string[] deletedAssets,
		string[] movedAssets,
		string[] movedFromAssetPaths)
	{
		var types = new string[] {".cs", ".js"};
		foreach(var i in importedAssets.Where(i => types.Contains(Path.GetExtension(i) ) ) ) 
		{
            UnityEditor.EditorApplication.delayCall += ()=>  ProcessScript(i);			
		}
	}

    static void ProcessScript(string path)
    {
        var script = UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(UnityEditor.MonoScript)) as UnityEditor.MonoScript;
        if(!script || script.GetClass()==null) return;

        var attrib = script.GetClass().GetCustomAttributes(typeof(ScriptExecutionOrderAttribute), true).Cast<ScriptExecutionOrderAttribute>().FirstOrDefault();
        if(attrib == null) return;

        var currentOrder = UnityEditor.MonoImporter.GetExecutionOrder(script);
        if(attrib.order == currentOrder) return;

        UnityEditor.MonoImporter.SetExecutionOrder(script, attrib.order);
    }
}
#endif
