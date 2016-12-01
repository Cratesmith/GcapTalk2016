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
    }

    void Update()
    {
        m_text.text = m_scoreManager.currentScore.ToString();
    }
}
