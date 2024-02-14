using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public delegate void ScoreUpdateCallback(List<int> newScores); 

public class ScoreSystem : GameSystem
{
    
    public ScoreUpdateCallback m_onScoreUpdate;
    private static ScoreSystem _instance;
    public static ScoreSystem Instance
    {
        get { return _instance;  }
    }

    public List<int> m_playerScores;

    void Awake()
    {

        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        } else {
            _instance = this;
        }
    }

    void Start()
    {
        m_playerScores = new List<int>();
        for (int i = 0; i < PlayerSystem.Instance.m_numTeams; i++)
        {
            m_playerScores.Add(0);
        }
        base.SystemReady();
    }

    public void UpdateScore(List<int> scoreDelta)
    {
        for (int i = 0; i < PlayerSystem.Instance.m_numTeams; i++)
        {
            m_playerScores[i] += scoreDelta[i];
        }
        
        m_onScoreUpdate(m_playerScores);
    }
}
