using UnityEngine;
using System.Collections;
    
[CreateAssetMenu(menuName="Managers/Score Manager")]
public class ScoreManager : Manager
{
    [SerializeField] int startingScore = 0;
    public int currentScore { get; private set;}

    public override void OnAwake()
    {
        base.OnAwake();
        currentScore = startingScore;
    }

    public void OnPointsCollected(int num)
    {
        currentScore += num;
    }
}
