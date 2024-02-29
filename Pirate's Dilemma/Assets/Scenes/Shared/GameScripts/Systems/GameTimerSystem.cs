using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;


public delegate void GameStartEvent();
public delegate void GameFinishEvent();
public delegate void GameTimerUpdateEvent(int currentTimerValueSeconds);

public class GameTimerSystem : GameSystem
{
    private static GameTimerSystem m_instance;
    public static GameTimerSystem Instance
    {
        get
        {
            return m_instance;
        }
    }
    
    [SerializeField] private int m_gameStartTimerSeconds = 3;
    
    [SerializeField] private int m_gameTimerSeconds = 120;

    [SerializeField] private string m_characterSelectSceneName;
    
    [SerializeField] private int m_krakenArrivalTimeRemaining = 60;
    
    public List<string> m_levelSceneNames;
    
    public GameStartEvent m_onGameStart;
    
    public GameFinishEvent m_onGameFinish;
    
    public GameTimerUpdateEvent m_onGameTimerUpdate;
    public GameTimerUpdateEvent m_onStartGameTimerUpdate;

    private bool m_krakenArrived = false;
    
    void Awake()
    {
        if (m_instance != null && m_instance != this)
        {
            Destroy(m_instance.gameObject);
        }
        else
        {
            m_instance = this;
        }
        
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        
        //idea here is that the scene manager only wants to register the callback if loading into player select screen.
        if (m_levelSceneNames.Contains(SceneManager.GetActiveScene().name))
        {
            SceneManager.sceneLoaded -= StartGame;
        }
        else if (SceneManager.GetActiveScene().name == m_characterSelectSceneName)
        {
            SceneManager.sceneLoaded += StartGame;
        }
        
        base.SystemReady();
    }

    public void StartGame(Scene scene, LoadSceneMode mode)
    {

        StartCoroutine(StartGameCountdown());
    }

    public void StopGame()
    {
        if (m_onGameFinish?.GetInvocationList()?.Length > 0)
        {
            m_onGameFinish();
        }
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
        
        Debug.Log("start game countdown complete!");
        StartCoroutine(GlobalCountdown(GameTimerSystem.Instance.m_gameTimerSeconds));
    }
    
    IEnumerator GlobalCountdown(int seconds)
    {
        int count = seconds;

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

        m_onGameFinish();
    }
    
}
