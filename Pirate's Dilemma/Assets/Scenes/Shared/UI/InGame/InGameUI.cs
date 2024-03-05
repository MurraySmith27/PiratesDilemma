using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class InGameUI : UIBase
{
    [SerializeField] private VisualTreeAsset m_scoreElementAsset;

    [SerializeField] private Sprite m_gameRulesTutorialSprite;

    [SerializeField] private GameObject m_tutorialPopupObject;
    
    [SerializeField] private bool m_useTutorialPopups = false;
    
    private Label m_globalTimerLabel;
    private Label m_leaderBoardLabel;
    private Label m_gameStartTimerLabel;
    
    private List<Label> m_teamScoreLabels;

    private bool m_showingTutorialPopup = false;

    private Sprite m_currentTutorialPopupSprite;

    private PlayerControlSchemes m_playerControlSchemes;
    

    protected override void Awake()
    {
        base.Awake();
    }
    
    protected override void SetUpUI()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        m_globalTimerLabel = root.Q<Label>("global-timer");
        m_leaderBoardLabel = root.Q<Label>("score-board-title");
        m_gameStartTimerLabel = root.Q<Label>("game-start-label");

        m_teamScoreLabels = new List<Label>();

        for (int i = 0; i < PlayerSystem.Instance.m_numTeams; i++)
        {
            m_teamScoreLabels.Add(m_scoreElementAsset.Instantiate().Q<Label>("score-label"));
            root.Q<VisualElement>("score-board").Add(m_teamScoreLabels[i]);
            m_teamScoreLabels[i].text = $"Team {i+1}: 0";
            m_teamScoreLabels[i].style.backgroundColor = PlayerSystem.Instance.m_teamColors[i];
        }
        
        m_leaderBoardLabel.text = "Scores:";

        m_showingTutorialPopup = false;
        
        ScoreSystem.Instance.m_onScoreUpdate += UpdateScoreUI;

        GameTimerSystem.Instance.m_onGameStart += OnGameStart;

        GameTimerSystem.Instance.m_onGamePause += OnGamePause;
        
        GameTimerSystem.Instance.m_onGameUnpause += OnGameUnpause;

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

    void OnUIPauseButtonPressed(InputAction.CallbackContext ctx)
    {
        if (GameTimerSystem.Instance.m_gamePaused && !m_showingTutorialPopup)
        {
            GameTimerSystem.Instance.UnPauseGame();
        }
        else
        {
            GameTimerSystem.Instance.PauseGame();
        }
    }

    void OnUISelectButtonPressed(InputAction.CallbackContext ctx)
    {
        if (m_showingTutorialPopup) {
            GameTimerSystem.Instance.UnPauseGame();
            m_tutorialPopupObject.GetComponent<TutorialPopupController>().HidePopup();
            m_currentTutorialPopupSprite = null;
            m_showingTutorialPopup = false;
        }
    }

    IEnumerator FlashTextOnScreen(string text, float timeAliveSeconds)
    {
        m_gameStartTimerLabel.text = text;
        yield return new WaitForSeconds(timeAliveSeconds);
        m_gameStartTimerLabel.text = "";
    }
    
    void OnGameTimerValueChange(int newValueSeconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(newValueSeconds);

        m_globalTimerLabel.text = time.ToString(@"m\:ss");
    }

    void OnGameStart()
    {
        Camera.main.GetComponent<AudioSource>().Play();

        if (m_useTutorialPopups)
        {
            m_tutorialPopupObject.GetComponent<TutorialPopupController>().ShowPopup(m_gameRulesTutorialSprite);

            m_currentTutorialPopupSprite = m_gameRulesTutorialSprite;
            m_showingTutorialPopup = true;

            GameTimerSystem.Instance.PauseGame();
        }
    }

    void OnGamePause()
    {

        if (!m_showingTutorialPopup)
        {
            m_gameStartTimerLabel.text = "Paused";
        }
    }
    
    void OnGameUnpause()
    {
        m_gameStartTimerLabel.text = "";
    }
    
    void OnGameFinish()
    {
        m_gameStartTimerLabel.text = "Time's Up!";
    }

    void UpdateScoreUI(List<int> newScores)
    {
        for (int i = 0; i < PlayerSystem.Instance.m_numTeams; i++)
        {
            m_teamScoreLabels[i].text = $"Team {i+1}: {newScores[i]}";
        }
    }
    
    void OnEnable()
    { 
        
        m_playerControlSchemes = new PlayerControlSchemes();

        m_playerControlSchemes.FindAction("Pause").performed += OnUIPauseButtonPressed;
        m_playerControlSchemes.FindAction("Pause").Enable();
        m_playerControlSchemes.FindAction("Select").performed += OnUISelectButtonPressed;
        m_playerControlSchemes.FindAction("Select").Enable();
    }

    void OnDisable()
    {
        m_playerControlSchemes.FindAction("Pause").performed -= OnUIPauseButtonPressed;
        m_playerControlSchemes.FindAction("Pause").Disable();
        m_playerControlSchemes.FindAction("Select").performed -= OnUISelectButtonPressed;
        m_playerControlSchemes.FindAction("Select").Disable();
        
        ScoreSystem.Instance.m_onScoreUpdate -= UpdateScoreUI;

        GameTimerSystem.Instance.m_onGameStart -= OnGameStart;

        GameTimerSystem.Instance.m_onGamePause -= OnGamePause;
        
        GameTimerSystem.Instance.m_onGameUnpause -= OnGameUnpause;

        GameTimerSystem.Instance.m_onStartGameTimerUpdate -= OnStartGameTimerValueChange;

        GameTimerSystem.Instance.m_onGameTimerUpdate -= OnGameTimerValueChange;
        
        GameTimerSystem.Instance.m_onGameFinish -= OnGameFinish;
    }
    
}
