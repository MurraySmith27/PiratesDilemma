using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InGameUI : UIBase
{
    [SerializeField] private VisualTreeAsset m_scoreElementAsset;
    
    private Label m_globalTimerLabel;
    private Label m_leaderBoardLabel;
    
    private List<Label> m_teamScoreLabels;
    

    protected override void Awake()
    {
        base.Awake();
    }
    
    protected override void SetUpUI()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        m_globalTimerLabel = root.Q<Label>("global-timer");
        m_leaderBoardLabel = root.Q<Label>("score-board-title");

        m_teamScoreLabels = new List<Label>();

        for (int i = 0; i < PlayerSystem.Instance.m_numTeams; i++)
        {
            m_teamScoreLabels.Add(m_scoreElementAsset.Instantiate().Q<Label>("score-label"));
            root.Q<VisualElement>("score-board").Add(m_teamScoreLabels[i]);
            m_teamScoreLabels[i].text = $"P{i}: 0";
            m_teamScoreLabels[i].style.backgroundColor = PlayerSystem.Instance.m_teamColors[i];
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
        for (int i = 0; i < PlayerSystem.Instance.m_numTeams; i++)
        {
            m_teamScoreLabels[i].text = $"P{i}: {newScores[i]}";
        }
    }
    
}
