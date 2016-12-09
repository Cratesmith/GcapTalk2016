using UnityEngine;
using System.Collections;
using UnityEngine.Events;

[CreateAssetMenu(menuName="Managers/Score Manager")]
public class ScoreManager : Manager
{
    [SerializeField] int startingScore = 0;
    public int currentScore { get; private set;}
    public UnityEvent onScoreChanged = new UnityEvent();

    public override void OnAwake()
    {
        base.OnAwake();
        currentScore = startingScore;
    }

    public void OnPointsCollected(int num)
    {
        currentScore += num;
        onScoreChanged.Invoke();
    }
}
