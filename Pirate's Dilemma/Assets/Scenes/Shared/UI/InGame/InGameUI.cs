using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class InGameUI : UIBase
{
    private static InGameUI _instance;

    public static InGameUI Instance
    {
        get
        {
            return _instance;
        }
    }
    
    [SerializeField] private VisualTreeAsset m_scoreElementAsset;

    [SerializeField] private Sprite m_gameRulesTutorialSprite;

    [SerializeField] private GameObject m_tutorialPopupObject;
    
    [SerializeField] private bool m_useTutorialPopups = false;

    [SerializeField] private float m_scoreIncreaseDelay = 2f; 
    
    [SerializeField] private List<Sprite> m_gameStartCountdownImages;

    [SerializeField] private List<Color> m_gameStartCountdownTints;

    [SerializeField] private float m_gameStartTimerFlashFinalSizeFactor = 1.3f;

    [SerializeField] private Sprite m_pauseGameImage;
    
    [SerializeField] private Color m_pauseGameImageTint;
    
    [SerializeField] private Sprite m_gameFinishImage;
    
    [SerializeField] private Color m_gameFinishImageTint;

    [SerializeField] private StudioEventEmitter m_goSoundEventEmitter;
    
    [SerializeField] private StudioEventEmitter m_countdownSoundEventEmitter;


    
    private Label m_globalTimerLabel;
    private Label m_leaderBoardLabel;
    private VisualElement m_gameStartTimerElement;
    private VisualElement m_fullScreenBannerElement;
    
    private List<Label> m_teamScoreLabels;

    private bool m_showingTutorialPopup = false;

    private Sprite m_currentTutorialPopupSprite;

    private PlayerControlSchemes m_playerControlSchemes;
    

    protected override void Awake()
    {

        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
        base.Awake();
    }
    
    protected override void SetUpUI()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        m_globalTimerLabel = root.Q<Label>("global-timer");
        m_leaderBoardLabel = root.Q<Label>("score-board-title");
        m_gameStartTimerElement = root.Q<VisualElement>("game-start-label");
        m_fullScreenBannerElement = root.Q<VisualElement>("full-banner-element");

        m_teamScoreLabels = new List<Label>();

        for (int i = 0; i < PlayerSystem.Instance.m_numTeams; i++)
        {
            VisualElement scoreElement = m_scoreElementAsset.Instantiate().Q<VisualElement>("root");
            m_teamScoreLabels.Add(scoreElement.Q<Label>("score-label"));
            root.Q<VisualElement>("score-board").Add(scoreElement);
            m_teamScoreLabels[i].text = $"0";
            m_teamScoreLabels[i].parent.style.backgroundColor = PlayerSystem.Instance.m_teamData[i].color;
        }
        
        m_leaderBoardLabel.text = "Scores:";

        m_showingTutorialPopup = false;
        
        ScoreSystem.Instance.m_onScoreUpdate += UpdateScoreUI;

        GameTimerSystem.Instance.m_onReadyToStartGameTimer += OnGameSceneLoaded;

        GameTimerSystem.Instance.m_onGameStart += OnGameStart;

        GameTimerSystem.Instance.m_onGameFreeze += OnGameFreeze;
        
        GameTimerSystem.Instance.m_onGameUnFreeze += OnGameUnFreeze;

        GameTimerSystem.Instance.m_onStartGameTimerUpdate += OnStartGameTimerValueChange;

        GameTimerSystem.Instance.m_onGameTimerUpdate += OnGameTimerValueChange;
        
        GameTimerSystem.Instance.m_onGameFinish += OnGameFinish;
    }

    public Vector2 GetScreenCoordinatesOfScoreboardEntry(int teamNum)
    {
        float left = m_teamScoreLabels[teamNum - 1].resolvedStyle.left;
        float top = m_teamScoreLabels[teamNum - 1].resolvedStyle.top;
        VisualElement parent = m_teamScoreLabels[teamNum - 1].parent;

        while (parent != null)
        {
            left += parent.resolvedStyle.left;
            top += parent.resolvedStyle.top;
            parent = parent.parent;
        }

        return new Vector2(left, Screen.height - top);
    }
    
    void OnStartGameTimerValueChange(int newValueSeconds)
    {
        if (newValueSeconds == 0)
        {
            m_goSoundEventEmitter.Play();
        }
        else
        {
            m_countdownSoundEventEmitter.Play();
        }
        StartCoroutine(FlashImageOnScreen(m_gameStartTimerElement, m_gameStartCountdownImages[newValueSeconds], m_gameStartCountdownTints[newValueSeconds], 0.95f));
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
            GameTimerSystem.Instance.UnFreezeGame();
            m_tutorialPopupObject.GetComponent<TutorialPopupController>().HidePopup();
            m_currentTutorialPopupSprite = null;
            m_showingTutorialPopup = false;
        }
    }

    IEnumerator FlashImageOnScreen(VisualElement element, Sprite image, Color imageTint, float timeAliveSeconds)
    {
        element.style.backgroundImage = image.texture;
        element.style.unityBackgroundImageTintColor = imageTint;

        Color initialTint = imageTint;
        Color finalTint = new Color(imageTint.r, imageTint.g, imageTint.b, 0f);

        float initialWidth = element.resolvedStyle.width;
        float initialHeight = element.resolvedStyle.height;
        
        float t = 0;
        while (t <= timeAliveSeconds)
        {
            float currentSize = (1f-t/timeAliveSeconds) + (m_gameStartTimerFlashFinalSizeFactor) * (t / timeAliveSeconds);
            Color currentTint = Color.Lerp(initialTint, finalTint, t / timeAliveSeconds);
            element.style.width = initialWidth * currentSize;
            element.style.height = initialHeight * currentSize;
            element.style.unityBackgroundImageTintColor = currentTint;
            t += Time.deltaTime;
            yield return null;
        }
        
        element.style.width = initialWidth;
        element.style.height = initialHeight;
        
        element.style.backgroundImage = null;
        element.style.unityBackgroundImageTintColor = Color.white;
    }
    
    void OnGameTimerValueChange(int newValueSeconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(newValueSeconds);

        m_globalTimerLabel.text = time.ToString(@"m\:ss");
    }

    void OnGameSceneLoaded()
    {
        if (m_useTutorialPopups)
        {
            Debug.Log("Freezing Game!");
            m_tutorialPopupObject.GetComponent<TutorialPopupController>().ShowPopup(m_gameRulesTutorialSprite);

            m_currentTutorialPopupSprite = m_gameRulesTutorialSprite;
            m_showingTutorialPopup = true;

            GameTimerSystem.Instance.FreezeGame();
        }
    }
    
    
    void OnGameStart()
    {
        Camera.main.GetComponent<AudioSource>().Play();
    }

    void OnGameFreeze()
    {

        if (!m_showingTutorialPopup)
        {
            m_gameStartTimerElement.style.backgroundImage = m_pauseGameImage.texture;
            m_gameStartTimerElement.style.unityBackgroundImageTintColor = m_pauseGameImageTint;
        }
    }
    
    void OnGameUnFreeze()
    {
        m_gameStartTimerElement.style.backgroundImage = null;
        m_gameStartTimerElement.style.unityBackgroundImageTintColor = Color.white;
    }
    
    void OnGameFinish()
    {
        StartCoroutine(FlashImageOnScreen(m_fullScreenBannerElement, m_gameFinishImage, m_gameFinishImageTint, 5f));
    }

    void UpdateScoreUI(List<int> newScores)
    {
        StartCoroutine(WaitThenUpdateScore(newScores));
    }

    private IEnumerator WaitThenUpdateScore(List<int> newScores)
    {
        yield return new WaitForSeconds(m_scoreIncreaseDelay);
        
        for (int i = 0; i < PlayerSystem.Instance.m_numTeams; i++)
        {
            m_teamScoreLabels[i].text = $"{newScores[i]}";
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
        
        GameTimerSystem.Instance.m_onReadyToStartGameTimer -= OnGameSceneLoaded;
        
        ScoreSystem.Instance.m_onScoreUpdate -= UpdateScoreUI;

        GameTimerSystem.Instance.m_onGameStart -= OnGameStart;

        GameTimerSystem.Instance.m_onGameFreeze -= OnGameFreeze;
        
        GameTimerSystem.Instance.m_onGameUnFreeze -= OnGameUnFreeze;

        GameTimerSystem.Instance.m_onStartGameTimerUpdate -= OnStartGameTimerValueChange;

        GameTimerSystem.Instance.m_onGameTimerUpdate -= OnGameTimerValueChange;
        
        GameTimerSystem.Instance.m_onGameFinish -= OnGameFinish;
    }
    
}
