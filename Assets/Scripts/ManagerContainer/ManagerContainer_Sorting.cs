using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Type = System.Type;

public partial class ManagerContainer : MonoBehaviour
{
    private List<Manager> GetSortedManagers()
    {
        Dictionary<Type, Manager>   lookup = new Dictionary<Type, Manager>();
        HashSet<Type>               visited = new HashSet<Type>();
        HashSet<Type>               sortedTypes = new HashSet<Type>();
        List<Manager>               sortedItems = new List<Manager>();
        for(int i=0;i<m_managerPrefabs.Length;++i)
        {
            var manager = m_managerPrefabs[i];
            if(manager==null) continue;
            lookup[manager.GetType()] = manager;
        }

        for(int i=0;i<m_managerPrefabs.Length;++i)
        {
            var manager = m_managerPrefabs[i];
            if(manager==null) continue;
            GetSortedManagers_Visit(manager.GetType(), visited, sortedItems, sortedTypes, lookup);
        }
        return sortedItems;
    }

    private void GetSortedManagers_Visit( Type current,
        HashSet<Type> visited,
        List<Manager> sortedItems,
        HashSet<Type> sortedTypes,
        Dictionary<Type, Manager> lookup)
    {
        if(visited.Add(current))
        {
            var deps = Manager.GetDependencies(current);
            for(int i=0;i<deps.Length; ++i)
            {
                GetSortedManagers_Visit( deps[i], visited, sortedItems, sortedTypes, lookup);
            }

            Manager item = null;
            if(lookup.TryGetValue(current, out item))
            {
                sortedItems.Add( item );
                sortedTypes.Add(current);
            }
        }
        else
        {
            Debug.Assert(sortedTypes.Contains(current), "Cyclic dependency found for manager"+current.Name+"!", this);
        }
    }
}
