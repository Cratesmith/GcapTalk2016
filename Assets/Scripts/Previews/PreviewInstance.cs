using UnityEngine;
using System.Collections;
using System.Linq;

#if UNITY_EDITOR
[ExecuteInEditMode]
[AddComponentMenu("")] // Hide this as you never want to create one manually!
public class PreviewInstance : MonoBehaviour, ISerializationCallbackReceiver
{    
    #region ISerializationCallbackReceiver implementation
    /// <summary>
    /// Destroys any prefab instances that are being written to prefabs.
    /// </summary>
    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
        #if UNITY_EDITOR        
        var prefabType = UnityEditor.PrefabUtility.GetPrefabType(gameObject);
        if(prefabType==UnityEditor.PrefabType.Prefab)
        {
            Debug.Log("Cleaning preview from prefab. There'll be an InstanceID!=0 error that's safe to ignore");
            GameObject.DestroyImmediate(gameObject, true);
        }
        #endif
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
    }
    #endregion


    /// <summary>
    /// Preview instances should always be set to hideanddontsave. 
    /// This awake ensures that.
    /// </summary>
    void Awake()
    {
        if (Application.isPlaying)
        {
            return;
        }

        gameObject.hideFlags = HideFlags.HideAndDontSave;
        var transforms = gameObject.GetComponentsInChildren<Transform>();
        foreach(var i in transforms)
        {
            i.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }   
    }

    /// <summary>
    /// Destroys all previewInstances in the scene
    /// </summary>
    [UnityEditor.Callbacks.PostProcessScene]
    static void PostPrfocessScene()
    {    
        var previews = Resources.FindObjectsOfTypeAll<PreviewInstance>()
            .Where(x=>string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(x)))
            .GetEnumerator();

        while(previews.MoveNext())
        {
            DestroyImmediate(previews.Current.gameObject);
        }
    }
}
#endif