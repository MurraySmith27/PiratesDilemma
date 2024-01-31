using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
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
    
    [SerializeField] private int m_gameTimerSeconds = 120;

    [SerializeField] private string m_characterSelectSceneName;

    public List<string> m_levelSceneNames;
    
    public GameStartEvent m_onGameStart;
    
    public GameFinishEvent m_onGameFinish;
    
    public GameTimerUpdateEvent m_onGameTimerUpdate;
    
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

    protected override void Start()
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
        
        
        base.Start();
    }

    public void StartGame(Scene scene, LoadSceneMode mode)
    {
        for (int i = 0; i < PlayerSystem.Instance.m_numPlayers; i++)
        {
            GameObject player = PlayerSystem.Instance.m_playerInputObjects[i].gameObject;

            PlayerData playerData = player.GetComponent<PlayerData>();

            playerData.m_playerNum = i + 1;
            playerData.m_teamNum = PlayerSystem.Instance.m_playerTeamAssignments[i];
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

        while (count > 0)
        {
            // Wait for one second
            yield return new WaitForSeconds(1);
            
            // Decrease the count
            count--;
            m_onGameTimerUpdate(count);
        }

        m_onGameFinish();
    }
}
