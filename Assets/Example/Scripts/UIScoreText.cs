using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class UIScoreText : BaseMonoBehaviour 
{
    ScoreManager m_scoreManager;
    Text         m_text;

    protected override void Awake()
    {
        base.Awake();
        m_scoreManager = GetManager<ScoreManager>();
        m_text = GetComponent<Text>();
        m_scoreManager.onScoreChanged.AddListener(UpdateScore);
        UpdateScore();
    }

    void UpdateScore()
    {
        m_text.text = m_scoreManager.currentScore.ToString();
    }
}
