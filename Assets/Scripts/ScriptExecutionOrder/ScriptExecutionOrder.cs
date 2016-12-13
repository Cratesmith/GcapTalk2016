//#define LOG_DEBUG
//#define LOG_DEBUG_VERBOSE

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Type = System.Type;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Linq;
#endif

/// <summary>
/// Sets the script exection order to a specific value.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Class)]
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
[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
public class ScriptDependencyAttribute : System.Attribute
{
    public readonly Type scriptDependencyType;
    public ScriptDependencyAttribute(Type type)
    {
        this.scriptDependencyType = type;
    }   

    public virtual Type[] GetScriptDependencies()
    {
        return new Type[] {scriptDependencyType};
    }
}
   
#if UNITY_EDITOR 
public static class ScriptExecutionOrder
{
    public class Builder : UnityEditor.AssetPostprocessor
    {
        static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) 
        {
            if(importedAssets
                .Concat(movedAssets)
                .Concat(deletedAssets)
                .Any(x=> x.EndsWith(".cs") || x.EndsWith(".js")))
            {
                ScriptExecutionOrder.ProcessAll();
            }
        }
    }

    [UnityEditor.Callbacks.DidReloadScripts]
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
            if(GetFixedOrder(script, out newOrder))
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

#if LOG_DEBUG            
            Debug.Log("ScriptExectionOrder: Island:"+i+" starts at "+newDepOrder
                +" Scripts:"+string.Join(", ", currentIsland
                    .Select(x=>(fixedOrders.ContainsKey(x.script) 
                        ? (x.script.name+"["+fixedOrders[x.script]+"]")
                        : x.script.name)+"("+x.isLeaf+")")
                    .ToArray()));
#endif


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
                    newDepOrder = isLeaf && !currentIsland.Skip(j+1).Any(x=>x.isLeaf)
                        ? Mathf.Max(0, newDepOrder) 
                        : Mathf.Max(-currentIsland.Length, newDepOrder);
                }   

                scriptOrders[script] = newDepOrder;

                // Leaves have no dependencies so the next behaviour can share the same execution order as a leaf
                // 
                if(!isLeaf || (isLeaf && !hasFixedOrderItem))
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
    /// Sort the scripts by dependencies 
    /// </summary>
    static List<IslandItem[]> SortDependencies(UnityEditor.MonoScript[] scriptsToSort)
    {
        var lookup = new Dictionary<Type, UnityEditor.MonoScript>();
        var visited = new HashSet<UnityEditor.MonoScript>();
        var sortedItems = new List<UnityEditor.MonoScript>();
        var connections = new Dictionary<UnityEditor.MonoScript, HashSet<UnityEditor.MonoScript>>();

        // add everything to lookup
        for(int i=0;i<scriptsToSort.Length;++i)
        {
            var script = scriptsToSort[i];
            if(script==null) continue;
            lookup[script.GetClass()] = script;
        }

        // build connection graph
        for (int i = 0; i < scriptsToSort.Length; ++i)
        {
            var script = scriptsToSort[i];
            if(script==null) continue;
            if(!HasDependencies(script)) continue;
            var deps = GetScriptDependencies(scriptsToSort[i]);
            for(int j=0; j < deps.Length; ++j)
            {
                var depType = deps[j];
                if(depType==null) continue;

                MonoScript depScript = null;
                if(!lookup.TryGetValue(depType, out depScript)) continue;

                // forward
                HashSet<UnityEditor.MonoScript> forwardSet = null;
                if (!connections.TryGetValue(script, out forwardSet))
                {
                    connections[script] = forwardSet = new HashSet<MonoScript>();
                }
                forwardSet.Add(depScript);
                
                // reverse
                HashSet<UnityEditor.MonoScript> reverseSet = null;
                if (!connections.TryGetValue(depScript, out reverseSet))
                {
                    connections[depScript] = reverseSet = new HashSet<MonoScript>();
                }
                reverseSet.Add(script);
            }           
        }

        // sort fixed order items first
        for(int i=0;i<scriptsToSort.Length;++i) 
        {
            var script = scriptsToSort[i];
            if(script==null || !HasDependencies(script)) continue;
            if(!HasFixedOrder(script)) continue;
            SortDependencies_Visit(script, visited, sortedItems, lookup, null, connections);
        }
        
        // non-leaves 
        for(int i=0;i<scriptsToSort.Length;++i) 
        {
            var script = scriptsToSort[i];
            if(script==null || !HasDependencies(script)) continue;

            HashSet<MonoScript> connectionSet = null;
            if (connections.TryGetValue(script, out connectionSet) 
                && GetScriptDependencies(script).Length == connections[script].Count)
            {
                continue;               
            }

            SortDependencies_Visit(script, visited, sortedItems, lookup, null, connections);
        }

        // leaves (any that remain)
        for(int i=0;i<scriptsToSort.Length;++i) 
        {
            var script = scriptsToSort[i];
            if(script==null || !HasDependencies(script)) continue;
            SortDependencies_Visit(script, visited, sortedItems, lookup, null, connections);
        }
         

        //Debug.Log("ScriptExecutionOrder: Sorted dependencies: "+string.Join(", ",sortedItems.Select(x=>x.name).ToArray()));
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
                        if(GetScriptDependencies(current).Length == currentConnections.Count)
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
        UnityEditor.MonoScript visitedBy,
        Dictionary<UnityEditor.MonoScript, HashSet<UnityEditor.MonoScript>> connections
        )
    {            
        if(visited.Add(current))  
        {  
            //
            // visit all dependencies (adding them recursively to the sorted list) before adding ourselves to the sorted list
            // this ensures that
            // 1. all dependencies are sorted
            // 2. cyclic dependencies can be caught if an item is visited AND it's been added to this list
            var depsRemaining = GetScriptDependencies(current);
             
            var visitedFrom = current;

            // do deps with fixed orders first
            for(int i=0;i<depsRemaining.Length; ++i)
            {
                SortDependencies_Visit_VisitDependency(visited, sortedItems, lookup, connections, depsRemaining[i], visitedFrom,
                    true,
                    false);
            }

            // then non-leaves
            for(int i=0;i<depsRemaining.Length; ++i)
            {
                SortDependencies_Visit_VisitDependency(visited, sortedItems, lookup, connections, depsRemaining[i], visitedFrom,
                    false,
                    false);
            }

            // then any leaves
            for(int i=0;i<depsRemaining.Length; ++i)
            {
                SortDependencies_Visit_VisitDependency(visited, sortedItems, lookup, connections, depsRemaining[i], visitedFrom,
                    false,
                    true);
            }

#if LOG_DEBUG_VERBOSE
            Debug.Log("Sorted "+current.name);
#endif
            sortedItems.Add( current ); 
        } 
        else
        {
            Debug.Assert(sortedItems.Contains(current), "Cyclic dependency found for ScriptDependency "+current.name+" via "+(visitedBy!=null?visitedBy.name:"Unknown")+"!");
        }
    }

    private static void SortDependencies_Visit_VisitDependency(HashSet<MonoScript> visited, List<MonoScript> sortedItems, Dictionary<Type, MonoScript> lookup,
        Dictionary<MonoScript, HashSet<MonoScript>> connections, Type depType, MonoScript visitedFrom, bool fixedOrderDeps, bool leafDeps)
    {
        if (depType == null) return;
        
        if (!typeof(ScriptableObject).IsAssignableFrom(depType) &&
            !typeof(MonoBehaviour).IsAssignableFrom(depType))
        {
            return;
        }

        MonoScript depScript = null;                   
        if (!lookup.TryGetValue(depType, out depScript))
        {
            Debug.LogError("ScriptDependency type " + depType.Name + " not found found for script " + visitedFrom.name +
                "! Check that it exists in a file with the same name as the class");
            return;
        }

        if (fixedOrderDeps && !HasFixedOrder(depScript))
        {
            return;
        }

        HashSet<MonoScript> connectionSet = null;
        if (leafDeps 
            && connections.TryGetValue(depScript, out connectionSet)
            && GetScriptDependencies(depScript).Length != connectionSet.Count)
        {
            return;               
        }

        if (lookup.TryGetValue(depType, out depScript)/* && !HasFixedOrder(depScript)*/)
        {
            SortDependencies_Visit(depScript, visited, sortedItems, lookup, visitedFrom, connections);
        }
    }

    /// <summary>
    /// Does this script have dependencies?
    /// </summary>
    static bool HasDependencies(UnityEditor.MonoScript script)
    {
        return GetScriptDependencies(script).Length > 0;
    }

    /// <summary>
    /// Does this script have fixed order?
    /// </summary>
    static bool HasFixedOrder(UnityEditor.MonoScript script)
    {
        int output = 0;
        return GetFixedOrder(script, out output);
    }

    /// <summary>
    /// Get the dependencies for a script using the lookup table
    /// </summary>
    private static Dictionary<MonoScript, Type[]> s_dependencyAttributes = new Dictionary<MonoScript, Type[]>();
    static Type[] GetScriptDependencies( UnityEditor.MonoScript script)
    {
        if(script==null) return new Type[0];

        var currentType = script.GetClass();
        if(currentType==null) return new Type[0];

        Type[] output = null;
        if (!s_dependencyAttributes.TryGetValue(script, out output))
        {
            List<Type> types = new List<Type>();

            var attribs = currentType.GetCustomAttributes(typeof(ScriptDependencyAttribute), true);
            for(int i=0;i<attribs.Length;++i)
            {
                var dep = attribs[i] as ScriptDependencyAttribute;
                if(dep==null) continue;               
                types.AddRange(dep.GetScriptDependencies()
                    .Where(x=>typeof(ScriptableObject).IsAssignableFrom(x) || typeof(MonoBehaviour).IsAssignableFrom(x))
                    );
            }
            s_dependencyAttributes[script] = output = types.ToArray();
        }         
        return output;
    }

    private static Dictionary<MonoScript, int?> s_fixedOrderAttributes = new Dictionary<MonoScript, int?>();
    static bool GetFixedOrder(MonoScript script, out int output)
    {
        output = 0;
        if (script == null) return false;

        int? value = null;

        if (!s_fixedOrderAttributes.TryGetValue(script, out value))
        {
            var order = UnityEditor.MonoImporter.GetExecutionOrder(script);
            output = order;

            var attrib = script.GetClass().GetCustomAttributes(typeof(ScriptExecutionOrderAttribute), true).Cast<ScriptExecutionOrderAttribute>().FirstOrDefault();
            if (attrib == null)
            {
                s_fixedOrderAttributes[script] = null;
            }
            else
            {
                s_fixedOrderAttributes[script] = value = attrib.order;    
            }            
        }

        if (value.HasValue)
        {
            output = value.Value;
            return true;
        }

        return false;
    }
}
#endif
