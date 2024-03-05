using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;


public delegate void GameStartEvent();
public delegate void GameFinishEvent();
public delegate void GamePauseEvent();
public delegate void GameUnpauseEvent();
public delegate void GameSceneLoadedEvent();
public delegate void GameSceneUnloadedEvent();
public delegate void GameTimerUpdateEvent(int currentTimerValueSeconds);

public class GameTimerSystem : GameSystem
{
    private static GameTimerSystem _instance;
    public static GameTimerSystem Instance
    {
        get
        {
            return _instance;
        }
    }
    
    [SerializeField] private int m_gameStartTimerSeconds = 3;
    
    [SerializeField] private int m_gameTimerSeconds = 120;

    public string m_characterSelectSceneName;
    
    [SerializeField] private int m_krakenArrivalTimeRemaining = 60;
    
    [SerializeField] private float m_holdAfterGameEndTime = 1f;
    
    [SerializeField] private float m_holdBeforeCountdownTimerTime = 1f;

    [SerializeField] private float m_gameSceneLoadedBufferSeconds = 0.5f;
    
    [SerializeField] private float m_gameSceneUnloadedBufferSeconds = 0.5f;

    [SerializeField] private string m_resultsSceneName;
    
    public List<string> m_levelSceneNames;
    
    public GameStartEvent m_onGameStart;
    
    public GameFinishEvent m_onGameFinish;

    public GamePauseEvent m_onGamePause;

    public GameUnpauseEvent m_onGameUnpause;
    
    public GameSceneLoadedEvent m_onGameSceneLoaded;
    
    public GameSceneUnloadedEvent m_onGameSceneUnloaded;
    
    public GameTimerUpdateEvent m_onGameTimerUpdate;
    public GameTimerUpdateEvent m_onStartGameTimerUpdate;
    
    public bool m_gamePaused = false;

    private bool m_krakenArrived = false;
    
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    void Start()
    {
        
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        base.SystemReady();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (m_levelSceneNames.Contains(SceneManager.GetActiveScene().name))
        {
            StartGame();
        }
    }

    public void PauseGame()
    {
        Time.timeScale = 0;
        m_gamePaused = true;
        m_onGamePause();
    }

    public void UnPauseGame()
    {
        Time.timeScale = 1f;
        m_gamePaused = false;
        m_onGameUnpause();
    }

    public void StartGame()
    {
        m_gamePaused = false;
        StartCoroutine(StartGameCoroutine());
    }

    private IEnumerator StartGameCoroutine()
    {
        m_onGameSceneLoaded();
        yield return new WaitForSeconds(m_gameSceneLoadedBufferSeconds);

        yield return new WaitForSeconds(m_holdBeforeCountdownTimerTime);
        
        StartCoroutine(StartGameCountdown());
        m_onGameTimerUpdate(m_gameTimerSeconds);
    }

    public void StopGame(string nextSceneToLoadName)
    {
        StartCoroutine(EndGameCoroutine(nextSceneToLoadName));
    }

    private IEnumerator StartGameCountdown()
    {
        int count = m_gameStartTimerSeconds;
        m_onStartGameTimerUpdate(count);

        while (count > 0)
        {
            // Wait for one second
            yield return new WaitForSeconds(1);
            
            // Decrease the count
            count--;
            m_onStartGameTimerUpdate(count);    
        }
        
        if (m_onGameStart?.GetInvocationList()?.Length > 0)
        {
            m_onGameStart();
        }
        
        StartCoroutine(GlobalCountdown(GameTimerSystem.Instance.m_gameTimerSeconds));
    }
    
    IEnumerator GlobalCountdown(int seconds)
    {
        int count = seconds;
        m_onGameTimerUpdate(count);

        while (count > 0)
        {
            // Wait for one second
            yield return new WaitForSeconds(1);
            
            // Decrease the count
            count--;

            if (count <= m_krakenArrivalTimeRemaining && !m_krakenArrived)
            {
                GameObject kraken = GameObject.FindGameObjectWithTag("Kraken");

                if (kraken != null)
                {
                    kraken.GetComponent<KrakenController>().StartKrakenArrival();
                }
                m_krakenArrived = true;
            }
            
            m_onGameTimerUpdate(count);
        }

        StartCoroutine(EndGameCoroutine(m_resultsSceneName));
    }

    private IEnumerator EndGameCoroutine(string nextSceneToLoadName)
    {
        //set time to half speed
        float endGameTimeScale = 0.5f;
        Time.timeScale = endGameTimeScale;
        
        m_onGameFinish();
        
        yield return new WaitForSeconds(m_holdAfterGameEndTime * endGameTimeScale);
        
        m_onGameSceneUnloaded();
        
        yield return new WaitForSeconds(m_gameSceneUnloadedBufferSeconds);
        
        SceneManager.LoadScene(nextSceneToLoadName);

        Time.timeScale = 1f;
    }
    
}
