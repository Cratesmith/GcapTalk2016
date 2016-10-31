using UnityEngine;
using System.Collections;

public partial class ScoreSettings : ResourceSingleton<ScoreSettings>
{
    [SerializeField] ActorCoinPickup.Settings m_coinPickup;
    public static ActorCoinPickup.Settings coinPickup { get { return instance.m_coinPickup; } }
}

[RequireComponent(typeof(PreviewModel))]
public class ActorCoinPickup : BaseMonoBehaviour, IPreviewModelSource
{
    [System.Serializable]
    public class Settings 
    {
        public int          numPoints = 100;
        public GameObject   modelPrefab;
    }

    GameObject m_modelInstance;

    void Awake()
    {
        if(ScoreSettings.coinPickup.modelPrefab) 
        {
            m_modelInstance = Instantiate(ScoreSettings.coinPickup.modelPrefab, 
                transform.position, 
                transform.rotation, 
                transform) as GameObject;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<ActorPlayer>();
        if(player!=null && enabled)
        {
            player.GivePoints(ScoreSettings.coinPickup.numPoints);
            Destroy(gameObject);
            enabled = false;
        }
    }

    #region IPreviewModelSource implementation
    GameObject IPreviewModelSource.previewModelPrefab
    {
        get { return ScoreSettings.coinPickup.modelPrefab; }
    }
    #endregion
} 