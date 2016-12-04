using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Type = System.Type;

#if UNITY_EDITOR
using System.IO;
using System.Linq;
#endif

/// <summary>
/// Sets the script exection order to a specific value.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.All)]
public class ScriptExecutionOrderAttribute : System.Attribute 
{
	public readonly int order;
	public ScriptExecutionOrderAttribute(int order)
	{
		this.order = order;
	}
}

/// <summary>
/// Ensures that this script will execute after all scripts of the specified type.
/// Exection order for all scripts in a dependency chain are automatically assigned.
/// This respects order values set by ScriptExecutionOrderAttribute, and will show a warning if that's not possible.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Class)]
public class ScriptDependencyAttribute : System.Attribute
{
    #if UNITY_EDITOR
    public readonly Type scriptDependencyType;
    public ScriptDependencyAttribute(Type type)
    {
        this.scriptDependencyType = type;
    }   

    public virtual Type[] GetScriptDependencies()
    {
        return new Type[] {scriptDependencyType};
    }
    #endif
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
             
        var scriptOrders = new Dictionary<UnityEditor.MonoScript, int>();
        var sortedDeps = SortDependencies(allScripts.ToArray());
        for(int i=0;i<sortedDeps.Count; ++i)
        {
            bool hasFixedOrderItem = false;

            //
            // find out the starting priority for this island 
            var currentIsland = sortedDeps[i];

            var newDepOrder = -currentIsland.Length;
            for(int j=0; j<currentIsland.Length; ++j)
            {
                var script = currentIsland[j].script;
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

            Debug.Log("ScriptExectionOrder: Island:"+i+" starts at "+newDepOrder
                +" Scripts:"+string.Join(",", currentIsland
                    .Select(x=>fixedOrders.ContainsKey(x.script) ? (x.script.name+"["+fixedOrders[x.script]+"]"):x.script.name)
                    .ToArray()));

            // 
            // apply priorities in order
            for(int j=0; j<currentIsland.Length; ++j)
            {               
                int scriptFixedOrder = 0;
                var script = currentIsland[j].script;
                var isLeaf = currentIsland[j].isLeaf;
                if(fixedOrders.TryGetValue(script, out scriptFixedOrder))
                {
                    newDepOrder = Mathf.Max(scriptFixedOrder, newDepOrder);
                    if(newDepOrder!=scriptFixedOrder)
                    {
                        Debug.LogWarning("ScriptExectionOrder: "+script.name+" has fixed exection order "+scriptFixedOrder+" but due to ScriptDependency sorting is now at order "+newDepOrder);
                    }
                    fixedOrders.Remove(script);
                } 
                else if(fixedOrders.Count==0)
                {
                    if(isLeaf)
                    {
                        newDepOrder = Mathf.Max(0, newDepOrder);
                    }
                    else 
                    {
                        newDepOrder = Mathf.Max(-currentIsland.Length, newDepOrder);
                    }
                }   

                scriptOrders[script] = newDepOrder;

                // Leaves have no dependencies so the next behaviour can share the same execution order as a leaf
                // 
                if(!isLeaf)
                {
                    ++newDepOrder;
                }
            }
        }

        foreach(var i in scriptOrders)
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

    /// <summary>
    /// Does this script have a fixed exection order from ScriptExectionOrderAttribute? If so what is it's value?
    /// </summary>
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

    /// <summary>
    /// Sort the scripts by dependencies 
    /// </summary>
    static List<IslandItem[]> SortDependencies(UnityEditor.MonoScript[] scriptsToSort)
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

        // sort fixed order items first
        for(int i=0;i<scriptsToSort.Length;++i) 
        {
            var script = scriptsToSort[i];
            if(script==null) continue;
            if(!SortDependencies_HasDependencies(script)) continue;
            if(!SortDependencies_HasFixedOrder(script)) continue;
            SortDependencies_Visit(script, visited, sortedItems, lookup, connections, null);
        }

        // then sort non-fixed ones
        for(int i=0;i<scriptsToSort.Length;++i)
        {
            var script = scriptsToSort[i];
            if(script==null) continue;
            if(!SortDependencies_HasDependencies(script)) continue;
            if(SortDependencies_HasFixedOrder(script)) continue;
            SortDependencies_Visit(script, visited, sortedItems, lookup, connections, null);
        }
         

        Debug.Log("ScriptExecutionOrder: Sorted dependencies: "+string.Join(", ",sortedItems.Select(x=>x.name).ToArray()));
        return SortDependencies_CreateGraphIslands(sortedItems, connections);
    }

    struct IslandItem
    {
        public UnityEditor.MonoScript script;
        public bool isLeaf;
    }

    /// <summary>
    /// Create graph islands from the non directed dependency graph
    /// </summary>
    static List<IslandItem[]> SortDependencies_CreateGraphIslands(List<UnityEditor.MonoScript> scriptsToSort, 
        Dictionary<UnityEditor.MonoScript, HashSet<UnityEditor.MonoScript>> connections)
    {
        var output = new List<IslandItem[]>(); 
        var remainingSet = new List<UnityEditor.MonoScript>(scriptsToSort);
        var leaves = new HashSet<UnityEditor.MonoScript>();

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
                        // leaves are scripts that no other scripts depend on
                        if(SortDependencies_GetDependencies(current).Length == currentConnections.Count)
                        {
                            leaves.Add(current);
                        }

                        if (graphComponentSet.Contains(connection))
                            continue;
                        openSet.Add(connection);
                    }
                }
                current = openSet.FirstOrDefault();
            }

            if (graphComponentSet.Count > 0)
            {
                var newIsland = scriptsToSort
                    .Where(graphComponentSet.Contains)
                    .Select(x=> new IslandItem() {script = x, isLeaf = leaves.Contains(x)})
                    .ToArray();
                output.Add(newIsland);
            }
        }

        return output;
    }

    /// <summary>
    /// Visit this script and all dependencies (adding them recursively to the sorted list).
    /// This also builds a connections table that can be used as a nondirected graph of dependencies
    /// </summary>
    static void SortDependencies_Visit( UnityEditor.MonoScript current,
        HashSet<UnityEditor.MonoScript> visited,
        List<UnityEditor.MonoScript> sortedItems,
        Dictionary<Type, UnityEditor.MonoScript> lookup,
        Dictionary<UnityEditor.MonoScript, HashSet<UnityEditor.MonoScript>> connections,
        UnityEditor.MonoScript visitedBy)
    {
        // 
        // Table all connections so islands can be calculated 
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
            //
            // visit all dependencies (adding them recursively to the sorted list) before adding ourselves to the sorted list
            // this ensures that
            // 1. all dependencies are sorted
            // 2. cyclic dependencies can be caught if an item is visited AND it's been added to this list
            var deps = SortDependencies_GetDependencies(current);

            // do deps with fixed orders first
            for(int i=0;i<deps.Length; ++i)
            {
                if(deps[i]==null) continue;
                UnityEditor.MonoScript depScript = null;
                Debug.Assert(lookup.ContainsKey(deps[i]), "ScriptDependency type "+deps[i].Name+" not found found for script "+current.name+"! Check that it exists in a file with the same name as the class");
                if(lookup.TryGetValue(deps[i], out depScript) && SortDependencies_HasFixedOrder(depScript))
                {
                    currentConnectionSet.Add(depScript);
                    SortDependencies_Visit(depScript, visited, sortedItems, lookup, connections, current);
                }
            }

            // then ones without fixed orders
            for(int i=0;i<deps.Length; ++i)
            {
                if(deps[i]==null) continue;
                UnityEditor.MonoScript depScript = null;
                Debug.Assert(lookup.ContainsKey(deps[i]), "ScriptDependency type "+deps[i].Name+" not found found for script "+current.name+"! Check that it exists in a file with the same name as the class");
                if(lookup.TryGetValue(deps[i], out depScript) && !SortDependencies_HasFixedOrder(depScript))
                {
                    currentConnectionSet.Add(depScript);
                    SortDependencies_Visit(depScript, visited, sortedItems, lookup, connections, current);
                }
            }

            sortedItems.Add( current );
        }
        else
        {
            Debug.Assert(sortedItems.Contains(current), "Cyclic dependency found for ScriptDependency "+current.name+" via "+(visitedBy!=null?visitedBy.name:"Unknown")+"!");
        }
    }

    /// <summary>
    /// Does this script have dependencies?
    /// </summary>
    static bool SortDependencies_HasDependencies(UnityEditor.MonoScript script)
    {
        if(script==null) return false;
        var attribs = script.GetClass().GetCustomAttributes(typeof(ScriptDependencyAttribute), true);
        return attribs.Length > 0;
    }

    /// <summary>
    /// Does this script have dependencies?
    /// </summary>
    static bool SortDependencies_HasFixedOrder(UnityEditor.MonoScript script)
    {
        if(script==null) return false;
        var attribs = script.GetClass().GetCustomAttributes(typeof(ScriptExecutionOrderAttribute), true);
        return attribs.Length > 0;
    }

    /// <summary>
    /// Get the dependencies for a script using the lookup table
    /// </summary>
    static Type[] SortDependencies_GetDependencies( UnityEditor.MonoScript current)
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
            types.AddRange(dep.GetScriptDependencies());
        }

        return types.ToArray();
    }
}
#endif
