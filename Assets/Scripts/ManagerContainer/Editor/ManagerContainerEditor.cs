using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(ManagerContainer))]
public class ManagerContainerEditor : Editor
{
    private List<Editor> editors;

    public override void OnInspectorGUI()
    {
        var container = (ManagerContainer)target;

        if (!Application.isPlaying)
        {
            base.OnInspectorGUI();
        }
        else
        {
            if (editors == null)
            {
                editors = new List<Editor>();
                var managerInstancesE = container.managerInstances.GetEnumerator();
                while (managerInstancesE.MoveNext())
                {
                    editors.Add(Editor.CreateEditor(managerInstancesE.Current));
                }
            }

            var editorsE = editors.GetEnumerator();
            while (editorsE.MoveNext())
            {
                var editor = editorsE.Current;
                if (editor != null)
                {
                    editor.DrawHeader();
                    editor.OnInspectorGUI();

                    if (editor.HasPreviewGUI())
                    {                        
                        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false),  GUILayout.Height(200));
                        var rect = GUILayoutUtility.GetLastRect();
                        editor.DrawPreview(rect);
                    }
                }
            }        
        }
    }
}