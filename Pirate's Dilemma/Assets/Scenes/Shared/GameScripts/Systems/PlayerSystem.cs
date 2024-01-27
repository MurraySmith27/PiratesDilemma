using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;


public delegate void OnPlayerJoin(int newPlayerNum);

//The purpose of this class to to store data that persists between scenes about players.
public class PlayerSystem : MonoBehaviour
{
    private static PlayerSystem _instance;
    public static PlayerSystem Instance
    {
        get { return _instance; }
    }
    
    private List<Transform> m_playerSpawnPositions;
    
    public List<Color> m_playerColors;
    
    public int m_maxNumPlayers = 4;

    [HideInInspector]
    public int m_numPlayers = 0;

    private List<PlayerControlSchemes> m_playerControlSchemesList;
    
    [HideInInspector]
    public List<PlayerInput> m_playerInputObjects;
    
    public OnPlayerJoin m_onPlayerJoin;
    
    private void Awake()
    {
        if (PlayerSystem._instance != null && PlayerSystem._instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            PlayerSystem._instance = this;
        }

        m_playerControlSchemesList = new List<PlayerControlSchemes>(m_maxNumPlayers);
        m_playerInputObjects = new List<PlayerInput>(m_maxNumPlayers);
        
        DontDestroyOnLoad(this.gameObject);    
    }

    private void Start()
    {
        this.SetPlayerSpawnPositions();
    }

    private void SetPlayerSpawnPositions()
    {
        m_playerSpawnPositions = new List<Transform>(new Transform[]
        {
            GameObject.FindGameObjectWithTag("P1Spawn").transform,
            GameObject.FindGameObjectWithTag("P2Spawn").transform,
            GameObject.FindGameObjectWithTag("P3Spawn").transform,
            GameObject.FindGameObjectWithTag("P4Spawn").transform
        });
    }

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        int playerNum = PlayerSystem.Instance.AddPlayer();
        if (playerNum == -1)
        {
            //at player limit. destroy.
            Destroy(this.gameObject);
        }

        playerInput.gameObject.transform.position = m_playerSpawnPositions[playerNum - 1].position;
        playerInput.gameObject.GetComponent<MeshRenderer>().material.color = m_playerColors[playerNum - 1];

        RegisterDeviceWithPlayer(playerNum, playerInput.devices[0]);
        m_playerInputObjects.Add(playerInput);
        
        DontDestroyOnLoad(playerInput.gameObject);
        m_onPlayerJoin(playerNum);
    }

    public int AddPlayer()
    {
        if (m_numPlayers < m_maxNumPlayers)
        {
            m_numPlayers++;
            
            m_playerControlSchemesList.Add(new PlayerControlSchemes());
                        
            return m_numPlayers;
        }
        else
        {
            return -1;
        }
    }

    public void RegisterDeviceWithPlayer(int playerNum, InputDevice device)
    {
        if (playerNum > m_numPlayers)
        {
            throw new ArgumentException($"Cannot change device for player number {playerNum}. " +
                                        $"There are only {m_numPlayers} players registered.");
        }

        m_playerControlSchemesList[playerNum - 1].devices = new[] { device };
    }

    public void DeregisterDeviceFromPlayer(int playerNum, InputDevice device)
    {
        if (playerNum > m_numPlayers)
        {
            throw new ArgumentException($"Cannot change device for player number {playerNum}. " +
                                        $"There are only {m_numPlayers} players registered.");
        }

        m_playerControlSchemesList[playerNum - 1].devices = null;
    }

    public void SwitchToActionMapForPlayer(int playerNum, string actionMapName)
    {
        m_playerInputObjects[playerNum - 1].actions.Disable();
        
        m_playerInputObjects[playerNum - 1].actions.FindActionMap(actionMapName).Enable();
    }
    
    public void OnDisable()
    {
        foreach (PlayerInput playerInput in m_playerInputObjects)
        {
            playerInput.actions.Disable();
        }
    }

    public void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (GameStartSystem.Instance.m_levelSceneNames.Contains(scene.name))
        {
            //get spawn positions from gameobject in scene with special tags.
            SetPlayerSpawnPositions();
            
            //move players to spawn positions
            for (int i = 0; i < m_numPlayers; i++)
            {
                GameObject spawnPos = GameObject.Find($"P{i + 1}Spawn");

                m_playerInputObjects[i].gameObject.transform.position = spawnPos.transform.position;
                
                SwitchToActionMapForPlayer(i + 1, "InGame");
            }
        }
    }
}
