using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName="Settings/ActorPlayer", fileName="ActorPlayerSettings.asset")]
public class ActorPlayerSettings : ScriptableObject
{
    public ActorPlayer.Settings settings;
}

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(ActorPlayerSettings))]
public class SettingsDrawer : PopupEditorDrawer {}
#endif