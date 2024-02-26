using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InGameUI : UIBase
{
    [SerializeField] private VisualTreeAsset m_scoreElementAsset;
    
    private Label m_globalTimerLabel;
    private Label m_leaderBoardLabel;
    private Label m_gameStartTimerLabel;
    
    private List<Label> m_teamScoreLabels;
    

    protected override void Awake()
    {
        base.Awake();
    }
    
    protected override void SetUpUI()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("root");

        m_globalTimerLabel = root.Q<Label>("global-timer");
        m_leaderBoardLabel = root.Q<Label>("score-board-title");
        m_gameStartTimerLabel = root.Q<Label>("game-start-label");

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

        GameTimerSystem.Instance.m_onStartGameTimerUpdate += OnStartGameTimerValueChange;

        GameTimerSystem.Instance.m_onGameTimerUpdate += OnGameTimerValueChange;
        
        GameTimerSystem.Instance.m_onGameFinish += OnGameFinish;
    }

    void OnStartGameTimerValueChange(int newValueSeconds)
    {
        string text = $"{newValueSeconds}";
        if (newValueSeconds == 0)
        {
            text = "GO!";
        }
        StartCoroutine(FlashTextOnScreen(text, 1f));
    }

    IEnumerator FlashTextOnScreen(string text, float timeAliveSeconds)
    {
        m_gameStartTimerLabel.text = text;
        yield return new WaitForSeconds(timeAliveSeconds);
        m_gameStartTimerLabel.text = "";
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
