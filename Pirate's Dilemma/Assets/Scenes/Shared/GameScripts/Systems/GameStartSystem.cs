using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.SceneManagement;


public delegate void OnGameStart();
public class GameStartSystem : MonoBehaviour
{
    public static GameStartSystem Instance
    {
        get
        {
            return m_instance;
        }
    }

    [SerializeField] private string m_characterSelectSceneName;

    public List<string> m_levelSceneNames;

    private static GameStartSystem m_instance;

    public OnGameStart m_onGameStart;
    
    
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

        //idea here is that the scene manager only wants to register the callback if loading into player select screen.
        if (m_levelSceneNames.Contains(SceneManager.GetActiveScene().name))
        {
            SceneManager.sceneLoaded -= StartGame;
        }
        else if (SceneManager.GetActiveScene().name == m_characterSelectSceneName)
        {
            SceneManager.sceneLoaded += StartGame;
        }
    }

    public void StartGame(Scene scene, LoadSceneMode mode)
    {
        for (int i = 0; i < PlayerSystem.Instance.m_numPlayers; i++)
        {
            GameObject player = PlayerSystem.Instance.m_playerInputObjects[i].gameObject;

            player.GetComponent<PlayerDataController>().m_playerNum = i;
        }

        if (m_onGameStart?.GetInvocationList()?.Length > 0)
        {
            m_onGameStart();
        }
    }
}
