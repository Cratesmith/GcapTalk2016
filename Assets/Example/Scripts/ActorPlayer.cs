using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

public partial class GlobalDefaults : ResourceSingleton<GlobalDefaults>
{
    [FormerlySerializedAs("m_playerSettings")]
    [SerializeField] ActorPlayer.Settings m_defaultPlayerSettings;
    public static ActorPlayer.Settings defaultPlayerSettings { get { return instance.m_defaultPlayerSettings; } }
}
    
[RequireComponent(typeof(PreviewModel))]
public class ActorPlayer : BaseMonoBehaviour, IPreviewModelSource
{
    #region settings
    [System.Serializable] 
    public class Settings
    {
        public GameObject   playerModelPrefab;
        public float        moveSpeed = 1f;
    }

    [SerializeField] ActorPlayerSettings m_settingsOverride;
    public Settings settings { get {return m_settingsOverride!=null ? m_settingsOverride.settings:GlobalDefaults.defaultPlayerSettings;} }
    #endregion

    #region IPreviewModelSource
    GameObject IPreviewModelSource.previewModelPrefab
    {
        get { return settings.playerModelPrefab; }
    }
    #endregion

    PlayerManager   m_playerManager;
    GameObject      m_playerModelInstance;

    #region lifecycle
    void Awake()
    {
        m_playerManager = GetManager<PlayerManager>();
        m_playerManager.currentPlayer = this;

        if(settings.playerModelPrefab)
        {
            m_playerModelInstance = Instantiate(settings.playerModelPrefab, 
                transform.position, 
                transform.rotation, 
                transform) 
                as GameObject;
        }
    }

    void Update()
    {
        transform.position += Input.GetAxis("Horizontal") * Vector3.right * settings.moveSpeed * Time.deltaTime;
    }
    #endregion

    public void GivePoints(int i)
    {
        Debug.Log("I got "+i+" points!");
        m_playerManager.OnPlayerCollectedPoints(i);
    }
}