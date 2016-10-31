using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

public class PopupEditorWindow : UnityEditor.EditorWindow
{
    SerializedProperty property;
    Editor editor;
    Vector2 scrollPosition;

    public static PopupEditorWindow Create(Object obj, Rect rect, Vector2 size)
    {
        var window = EditorWindow.CreateInstance<PopupEditorWindow>() as PopupEditorWindow;
        window.editor = Editor.CreateEditor(obj);
        window.ShowAsDropDown(rect,size);
        window.minSize = size;
        return window;
    }

    void OnGUI()
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, "button");
        editor.DrawHeader();
        editor.OnInspectorGUI();
        GUILayout.EndScrollView();

        if(GUI.Button(new Rect(0,0,27,27), "", "TL SelectionBarCloseButton"))
        {
            Close();
        }
    }
}

[CustomPropertyDrawer(typeof(Animator))]
[CustomPropertyDrawer(typeof(Collider))]
[CustomPropertyDrawer(typeof(Collider2D))]
[CustomPropertyDrawer(typeof(NavMeshAgent))]
[CustomPropertyDrawer(typeof(Collider))]
public class PopupEditorDrawer : UnityEditor.PropertyDrawer
{
    const int HEADER_HEIGHT = 17;
    const int EDITOR_HEIGHT = 500;

    PopupEditorWindow window; 

    public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
    {
        return HEADER_HEIGHT;
    }

    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        var foldoutRect = new Rect(position.position.x-HEADER_HEIGHT+3 + EditorGUI.indentLevel*15, position.position.y, HEADER_HEIGHT, HEADER_HEIGHT-4);
        EditorGUI.BeginChangeCheck();
        bool foldout = window!=null;
        if(!foldout)
        {
            if(property.objectReferenceValue == null || property.hasMultipleDifferentValues)
            {
                foldout = false;
            }
            else 
            {
                foldout = GUI.Button(foldoutRect, "", "OL Plus");
            }
        }

        var objFieldRect = new Rect(position.x, position.y, position.width, HEADER_HEIGHT);
        EditorGUI.ObjectField(objFieldRect, property, label);

        if(foldout)
        {               
            var rect = new Rect(new Vector2(position.position.x, position.position.y+HEADER_HEIGHT), new Vector2(position.width, EDITOR_HEIGHT));
            //Debug.Log(rect.ToString()); 
            rect.position = EditorGUIUtility.GUIToScreenPoint(rect.position);           

            if(window==null)
            {
                window = PopupEditorWindow.Create(property.objectReferenceValue, position, new Vector2(position.width, EDITOR_HEIGHT));
            }

            if(window)
            {
                window.position = rect;
            }
        }
        else 
        {
            if(window)
            {
                window.Close();
                window = null;
            }
        }
    }
}

#endif