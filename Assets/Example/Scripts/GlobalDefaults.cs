using UnityEngine;
using System.Collections;

public partial class GlobalDefaults : ResourceSingleton<GlobalDefaults>
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Edit/Game Settings/Global Defaults")]
    public static void SelectSettings()
    {
        UnityEditor.Selection.activeObject = instance;
    }
    #endif

}