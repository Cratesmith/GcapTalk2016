﻿using UnityEngine;
using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Type = System.Type;

[System.AttributeUsage(System.AttributeTargets.All)]
public class ScriptExecutionOrderAttribute : System.Attribute 
{
	public readonly int order;
	public ScriptExecutionOrderAttribute(int order)
	{
		this.order = order;
	}
}

[System.AttributeUsage(System.AttributeTargets.Class)]
public class ScriptDependencyAttribute : System.Attribute
{
    public readonly Type type;

    public ScriptDependencyAttribute(Type type)
    {
        this.type = type;
    }   

    public Type[] GetDependencies()
    {
        return new Type[] {type};
    }
}
   
#if UNITY_EDITOR 
public static class ScriptExecutionOrder
{
    [UnityEditor.InitializeOnLoadMethod]
    static void ProcessAll()
    {
        var types = new string[] {".cs", ".js"};
        var fixedOrders = new Dictionary<UnityEditor.MonoScript, int>();

        var allScriptPaths = Directory.GetFiles("Assets", "*.*", SearchOption.AllDirectories)
            .Where(s => types.Any(x=>s.EndsWith(x, System.StringComparison.CurrentCultureIgnoreCase)))
            .ToArray();

        var allScripts = new List<UnityEditor.MonoScript>();
        for(int i=0;i<allScriptPaths.Length; ++i)
        {
            UnityEditor.MonoScript script = UnityEditor.AssetDatabase.LoadAssetAtPath(allScriptPaths[i], typeof(UnityEditor.MonoScript)) as UnityEditor.MonoScript;
            if(!script || script.GetClass()==null) continue;
            int newOrder = 0;
            if(DetectFixedOrderByAttribute(script, out newOrder))
            {
                fixedOrders[script] = newOrder;
            }
            allScripts.Add(script);
        }
             
        var newDepOrder = 0;
        var sortedDeps = SortDependencies(allScripts.ToArray());
        for(int i=0;i<sortedDeps.Count; ++i)
        {
            bool hasFixedOrderItem = false;

            //
            // find out the starting priority for this island 
            var currentIsland = sortedDeps[i];
            newDepOrder = 0;
            for(int j=0; j<currentIsland.Length; ++j)
            {
                var script = currentIsland[j];
                int scriptOrder = 0;
                if(fixedOrders.TryGetValue(script, out scriptOrder))
                {
                    // -j due to sorted items before it
                    newDepOrder = Mathf.Min(scriptOrder-j, newDepOrder);
                    hasFixedOrderItem = true;
                }
            }

            //
            // Don't edit execution order unless there's a fixed order or a dependency
            // This allows the script exection order UI to work normally for these cases 
            // instead of forcing them to exection order 0
            if(currentIsland.Length==1 && !hasFixedOrderItem)
            {
                continue;
            }

            // 
            // apply priorities in order
            for(int j=0; j<currentIsland.Length; ++j)
            {               
                int scriptFixedOrder = 0;
                var script = currentIsland[j];
                if(fixedOrders.TryGetValue(script, out scriptFixedOrder))
                {
                    newDepOrder = Mathf.Max(scriptFixedOrder, newDepOrder);
                    if(newDepOrder!=scriptFixedOrder)
                    {
                        Debug.LogWarning("ScriptExectionOrder: "+script.name+" has fixed exection order "+scriptFixedOrder+" but due to dependency sorting is now at order "+newDepOrder);
                    }
                } 

                fixedOrders[script] = newDepOrder;
                ++newDepOrder;
            }
        }

        foreach(var i in fixedOrders)
        {
            var script = i.Key;
            var order = i.Value;
            var currentOrder = UnityEditor.MonoImporter.GetExecutionOrder(script);
            if(order != currentOrder) 
            {
                UnityEditor.MonoImporter.SetExecutionOrder(script, order);
            }
        }
    }

    static bool DetectFixedOrderByAttribute(UnityEditor.MonoScript script, out int order)
    {
        order = 0;
        //var script = UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(UnityEditor.MonoScript)) as UnityEditor.MonoScript;
        if(!script || script.GetClass()==null)
        {
            return false;
        }

        order = UnityEditor.MonoImporter.GetExecutionOrder(script);
        var attrib = script.GetClass().GetCustomAttributes(typeof(ScriptExecutionOrderAttribute), true).Cast<ScriptExecutionOrderAttribute>().FirstOrDefault();
        if(attrib == null) 
        {
            return false;
        }

        order = attrib.order;
        return true;
    }

    static List<UnityEditor.MonoScript[]> SortDependencies(UnityEditor.MonoScript[] scriptsToSort)
    {
        var lookup = new Dictionary<Type, UnityEditor.MonoScript>();
        var visited = new HashSet<UnityEditor.MonoScript>();
        var sortedItems = new List<UnityEditor.MonoScript>();
        var connections = new Dictionary<UnityEditor.MonoScript, HashSet<UnityEditor.MonoScript>>();

        for(int i=0;i<scriptsToSort.Length;++i)
        {
            var script = scriptsToSort[i];
            if(script==null) continue;
            lookup[script.GetClass()] = script;
        }

        for(int i=0;i<scriptsToSort.Length;++i)
        {
            var script = scriptsToSort[i];
            if(script==null) continue;
            if(!SortDependencies_HasDependencies(script)) continue;
            SortDependencies_Visit(script, visited, sortedItems, lookup, connections, null);
        }
            
        return SortDependencies_CreateGraphIslands(scriptsToSort, connections);
    }

    static List<UnityEditor.MonoScript[]> SortDependencies_CreateGraphIslands(UnityEditor.MonoScript[] scriptsToSort, 
        Dictionary<UnityEditor.MonoScript, HashSet<UnityEditor.MonoScript>> connections)
    {
        var output = new List<UnityEditor.MonoScript[]>();
        var remainingSet = new HashSet<UnityEditor.MonoScript>(scriptsToSort);

        while (remainingSet.Any())
        {
            var graphComponentSet = new HashSet<UnityEditor.MonoScript>();
            var openSet = new HashSet<UnityEditor.MonoScript>();
            var current = remainingSet.First();
            while (current != null)
            {
                openSet.Remove(current);
                remainingSet.Remove(current);
                graphComponentSet.Add(current);
                HashSet<UnityEditor.MonoScript> currentConnections = null;
                if (connections.TryGetValue(current, out currentConnections))
                {
                    foreach (var connection in currentConnections)
                    {
                        if (graphComponentSet.Contains(connection))
                            continue;
                        openSet.Add(connection);
                    }
                }
                current = openSet.FirstOrDefault();
            }
            if (graphComponentSet.Count > 0)
            {
                output.Add(graphComponentSet.ToArray());
            }
        }

        return output;
    }

    static void SortDependencies_Visit( UnityEditor.MonoScript current,
        HashSet<UnityEditor.MonoScript> visited,
        List<UnityEditor.MonoScript> sortedItems,
        Dictionary<Type, UnityEditor.MonoScript> lookup,
        Dictionary<UnityEditor.MonoScript, HashSet<UnityEditor.MonoScript>> connections,
        UnityEditor.MonoScript visitedBy)
    {
        HashSet<UnityEditor.MonoScript> currentConnectionSet = null;
        if(!connections.TryGetValue(current, out currentConnectionSet))
        {
            connections[current] = currentConnectionSet = new HashSet<UnityEditor.MonoScript>();
        }

        if(visitedBy!=null)
        {
            currentConnectionSet.Add(visitedBy);
        }

        if(visited.Add(current))  
        {  
            var deps = SortDependencies_GetDependencies(current, lookup);
            for(int i=0;i<deps.Length; ++i)
            {
                if(deps[i]==null) continue;

                UnityEditor.MonoScript depScript = null;
                Debug.Assert(lookup.ContainsKey(deps[i]), "Dependency type "+deps[i].Name+" not found found for script "+current.name+"! Check that it exists in a file with the same name as the class");
                if(lookup.TryGetValue(deps[i], out depScript))
                {
                    currentConnectionSet.Add(depScript);
                    SortDependencies_Visit(depScript, visited, sortedItems, lookup, connections, current);
                }
            }

            sortedItems.Add( current );
        }
        else
        {
            Debug.Assert(sortedItems.Contains(current), "Cyclic dependency found for script "+current.name+"!");
        }
    }

    static bool SortDependencies_HasDependencies(UnityEditor.MonoScript script)
    {
        if(script==null) return false;
        var attribs = script.GetClass().GetCustomAttributes(typeof(ScriptDependencyAttribute), true);
        return attribs.Length > 0;
    }

    static Type[] SortDependencies_GetDependencies( UnityEditor.MonoScript current, Dictionary<Type, UnityEditor.MonoScript> lookup)
    {
        if(current==null) return new Type[0];

        var currentType = current.GetClass();
        if(currentType==null) return new Type[0];

        List<Type> types = new List<Type>();

        var attribs = currentType.GetCustomAttributes(typeof(ScriptDependencyAttribute), true);
        for(int i=0;i<attribs.Length;++i)
        {
            var dep = attribs[i] as ScriptDependencyAttribute;
            if(dep==null) continue;
            types.AddRange(dep.GetDependencies());
        }

        return types.ToArray();
    }
}
#endif
