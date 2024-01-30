using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InGameUI : UIBase
{
    private Label m_globalTimerLabel;
    private Label m_leaderBoardLabel;
    
    private List<Label> m_playerScoreElements;


    protected override void Awake()
    {
        base.Awake();
    }
    
    protected override void SetUpUI()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        m_globalTimerLabel = root.Q<Label>("global-timer");
        m_leaderBoardLabel = root.Q<Label>("score-board-title");

        m_playerScoreElements = new List<Label>();

        for (int i = 0; i < PlayerSystem.Instance.m_numPlayers; i++)
        {
            m_playerScoreElements.Add(root.Q<Label>($"player-{i + 1}-score"));
            m_playerScoreElements[i].text = $"P{i}: 0";
            m_playerScoreElements[i].style.backgroundColor = PlayerSystem.Instance.m_playerColors[i];
        }
        
        m_leaderBoardLabel.text = "Scores:";

        GameObject[] boats = GameObject.FindGameObjectsWithTag("Boat");

        ScoreSystem.Instance.m_onScoreUpdate += UpdateScoreUI;

        GameTimerSystem.Instance.m_onGameTimerUpdate += OnGameTimerValueChange;
        
        GameTimerSystem.Instance.m_onGameFinish += OnGameFinish;
    }

    void OnGameTimerValueChange(int newValueSeconds)
    {
        // Update the UI
        m_globalTimerLabel.text = $"TIME REMAINING: {newValueSeconds}";
    }

    void OnGameFinish()
    {
        m_globalTimerLabel.text = "Time's Up!";
    }

    void UpdateScoreUI(List<int> newScores)
    {
        for (int i = 0; i < PlayerSystem.Instance.m_numPlayers; i++)
        {
            m_playerScoreElements[i].text = $"P{i}: {newScores[i]}";
        }
    }
    
}
