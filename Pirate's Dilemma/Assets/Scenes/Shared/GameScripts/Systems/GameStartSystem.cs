using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.SceneManagement;


public delegate void OnGameStart();
public class GameStartSystem : MonoBehaviour
{

    private static GameStartSystem m_instance;

    public static GameStartSystem Instance
    {
        get
        {
            return m_instance;
        }
    }
    
    public OnGameStart onGameStart;
    
    void Start()
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

    public void StartGame(Scene scene, LoadSceneMode mode)
    {
        for (int i = 0; i < PlayerSystem.Instance.m_numPlayers; i++)
        {
            GameObject player = PlayerSystem.Instance.m_playerInputObjects[i].gameObject;

            player.GetComponent<PlayerDataController>().m_playerNum = i;
        }
        
        onGameStart();
    }
}
