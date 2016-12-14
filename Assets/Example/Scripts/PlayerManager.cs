using UnityEngine;
using System.Collections;
    
[CreateAssetMenu(menuName="Managers/Player Manager")]
[ManagerDependency(typeof(ScoreManager))]
public class PlayerManager : Manager
{
    [SerializeField] int pointsMultiplier = 1;
    public ActorPlayer currentPlayer {get;set;}

    ScoreManager m_scoreManager;

    public override void OnAwake()
    {
        base.OnAwake();
        m_scoreManager = GetManager<ScoreManager>();
    }

    public void OnPlayerCollectedPoints(int num)
    {
        m_scoreManager.OnPointsCollected(num*pointsMultiplier);
    }
}
