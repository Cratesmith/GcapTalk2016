using UnityEngine;
using System.Collections;

public partial class ScoreSettings : ResourceSingleton<ScoreSettings>
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Edit/Game Settings/Score Settings")]
    public static void SelectSettings()
    {
        UnityEditor.Selection.activeObject = instance;
    }
    #endif
}
