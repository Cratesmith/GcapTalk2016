using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
#if UNITY_EDITOR
[ScriptDependency(typeof(PreviewInstance))]
#endif
public class PreviewModel : MonoBehaviour
{
    #if UNITY_EDITOR
    [SerializeField] [HideInInspector] PreviewInstance m_previewInstance;
    public PreviewInstance previewInstance { get { return m_previewInstance;} }

    void Awake()
    {
        if(!Application.isPlaying)
        {
            UpdatePreview();
        }
        else 
        {
            DestroyPreview();
            this.enabled = false;
        }
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            return;
        }

        UpdatePreview();
    }

    /// <summary>
    /// Updates the preview if it is missing or out of date.
    /// </summary>
    void UpdatePreview()
    {
        var prefabType = UnityEditor.PrefabUtility.GetPrefabType(gameObject);
        if(prefabType==UnityEditor.PrefabType.Prefab)
        {
            return;
        }

        if(m_previewInstance ==null || IsPreviewOutOfDate())
        {
            DestroyPreview();
            var so = new UnityEditor.SerializedObject(this);
            so.FindProperty("m_previewInstance").objectReferenceValue = CreatePreviewInstance();
            so.ApplyModifiedProperties();
        }
    }       

    void DestroyPreview()
    {
        if (m_previewInstance!=null)
        {
            GameObject.DestroyImmediate(m_previewInstance.gameObject);
            m_previewInstance = null;
        }
    }

    protected GameObject previewPrefab
    {
        get 
        {
            var source = GetComponent<IPreviewModelSource>();
            return source !=null ? source.previewModelPrefab:null;
        }
    }

    protected PreviewInstance CreatePreviewInstance()  
    {
        if(previewPrefab==null)
        {   
            return null;
        }

        var previewObject = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(previewPrefab);
        var instance = previewObject.AddComponent<PreviewInstance>();
        instance.transform.parent = transform.transform;
        instance.transform.localPosition = previewPrefab.transform.localPosition;
        instance.transform.localRotation = previewPrefab.transform.localRotation;
        instance.transform.localScale = previewPrefab.transform.localScale;

        return instance;
    }

    protected bool IsPreviewOutOfDate()                
    {
        if(previewInstance==null)
        {
            return true;
        }

        var prefabParent = UnityEditor.PrefabUtility.GetPrefabParent(previewInstance.gameObject);
        if(previewPrefab==null || prefabParent!=previewPrefab)
        {
            return true;
        }
        return false;
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawIcon(transform.position, "Preview Icon", true);
    }   
    #endif
}

