using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public delegate void OnPlayerJoin(int newPlayerNum);

//The purpose of this class to to store data that persists between scenes about players.
public class PlayerSystem : GameSystem
{
    private static PlayerSystem _instance;
    public static PlayerSystem Instance
    {
        get { return _instance; }
    }
    
    public int m_maxNumPlayers = 4;
    
    public List<Color> m_playerColors;
    
    public int m_numTeams = 2;

    [HideInInspector] public int m_numPlayers = 0;

    [HideInInspector]
    public List<PlayerInput> m_playerInputObjects
    {
        get
        {
            return m_players.Select(player => player.GetComponent<PlayerInput>()).ToList();
        }
    }

    [HideInInspector] public List<GameObject> m_players;

    private List<Transform> m_playerSpawnPositions;

    private List<PlayerControlSchemes> m_playerControlSchemesList;
    
    [HideInInspector] public List<int> m_playerTeamAssignments;
    
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
        m_players = new List<GameObject>();
        m_playerTeamAssignments = new List<int>();
        
        DontDestroyOnLoad(this.gameObject);    
    }

    protected override void Start()
    {
        SetPlayerSpawnPositions();
        SceneManager.sceneLoaded += OnGameSceneLoaded;

        base.Start();
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
        int playerNum = this.AddPlayer();
        if (playerNum == -1)
        {
            //at player limit. destroy.
            Destroy(this.gameObject);
        }

        playerInput.gameObject.transform.position = m_playerSpawnPositions[playerNum - 1].position;
        playerInput.gameObject.GetComponent<MeshRenderer>().material.color = m_playerColors[playerNum - 1];

        RegisterDeviceWithPlayer(playerNum, playerInput.devices[0]);
        m_players.Add(playerInput.gameObject);
        
        DontDestroyOnLoad(playerInput.gameObject);
        m_onPlayerJoin(playerNum);
    }

    int AddPlayer()
    {
        if (m_numPlayers < m_maxNumPlayers)
        {
            m_numPlayers++;
            
            m_playerControlSchemesList.Add(new PlayerControlSchemes());
            
            //assign player to team. Initially whichever team has the least.
            List<int> numPlayersPerTeam = new List<int>();

            for (int i = 0; i < m_numTeams; i++)
            {
                numPlayersPerTeam.Add(0);
            }
            
            foreach (int playerTeamAssignment in m_playerTeamAssignments)
            {
                numPlayersPerTeam[playerTeamAssignment]++;
            }


            int smallestTeamNum = 1;
            int numPlayersOnSmallestTeam = numPlayersPerTeam[0];
            for (int i = 1; i < m_numTeams; i++)
            {
                if (numPlayersPerTeam[i] < numPlayersOnSmallestTeam)
                {
                    smallestTeamNum = i + 1;
                    numPlayersOnSmallestTeam = numPlayersPerTeam[i];
                }
            }
            
            m_playerTeamAssignments.Add(smallestTeamNum);
                        
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

    private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //only call the callback for the next scene loaded.
        SceneManager.sceneLoaded -= OnGameSceneLoaded;
        
        if (GameTimerSystem.Instance.m_levelSceneNames.Contains(scene.name))
        {
            //get spawn positions from gameobject in scene with special tags.
            SetPlayerSpawnPositions();
            
            //move players to spawn positions
            for (int i = 0; i < m_numPlayers; i++)
            {
                GameObject spawnPos = GameObject.Find($"P{i + 1}Spawn");

                m_players[i].transform.position = spawnPos.transform.position;
                
                SwitchToActionMapForPlayer(i + 1, "InGame");
            }
        }
    }
}
