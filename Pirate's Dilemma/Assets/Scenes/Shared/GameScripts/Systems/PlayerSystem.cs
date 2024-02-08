using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public delegate void PlayerDieEvent(int playerNum);
public delegate void PlayerRespawnEvent(int playerNum);
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
    
    public List<Color> m_teamColors;
    
    public int m_numTeams = 2;

    [HideInInspector] public int m_numPlayers = 0;

    [SerializeField] private float m_deathAnimationDistance = 5f;

    [SerializeField] private float m_deathAnimationDuration = 2f;

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

    [SerializeField] private float m_playerRespawnTime = 3f;

    private List<PlayerControlSchemes> m_playerControlSchemesList;
    
    [HideInInspector] public List<int> m_playerTeamAssignments;
    
    public OnPlayerJoin m_onPlayerJoin;
    
    public PlayerDieEvent m_onPlayerDie;

    public PlayerRespawnEvent m_onPlayerRespawn;
    
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
        int playerNum, teamNum;
        (playerNum, teamNum) = this.AddPlayer();
        if (playerNum == -1)
        {
            //at player limit. destroy.
            Destroy(this.gameObject);
        }

        playerInput.gameObject.transform.position = m_playerSpawnPositions[playerNum - 1].position;
        foreach (MeshRenderer meshRenderer in playerInput.gameObject.transform.GetChild(0).GetComponentsInChildren<MeshRenderer>())
        {
            meshRenderer.material.color = m_teamColors[teamNum - 1];
        }

        playerInput.gameObject.GetComponent<PlayerMovementController>().m_onPlayerDie += OnPlayerDie;

        RegisterDeviceWithPlayer(playerNum, playerInput.devices[0]);
        
        m_players.Add(playerInput.gameObject);
        
        DontDestroyOnLoad(playerInput.gameObject);
        m_onPlayerJoin(playerNum);
    }

    private void OnPlayerDie(int playerNum)
    {
        StartCoroutine(WaitForRespawn(playerNum));

        m_onPlayerDie(playerNum);
    }

    private IEnumerator WaitForRespawn(int playerNum)
    {
        m_players[playerNum - 1].GetComponent<PlayerMovementController>().enabled = false;
        PlayerGoldController playerGoldController = m_players[playerNum - 1].GetComponent<PlayerGoldController>();
        playerGoldController.enabled = false;
        if (playerGoldController.m_goldCarried > 0)
        {
            playerGoldController.DropAllGold();
        }

        Coroutine deathCoroutine = StartCoroutine(DeathAnimation(playerNum));
     
        yield return new WaitForSeconds(m_playerRespawnTime);
        //if still running, kill it
        StopCoroutine(deathCoroutine);
        
        m_players[playerNum - 1].GetComponent<PlayerMovementController>().enabled = true;
        m_players[playerNum - 1].GetComponent<PlayerGoldController>().enabled = true;

        m_players[playerNum - 1].transform.position = m_playerSpawnPositions[playerNum - 1].position;

        Rigidbody rb = m_players[playerNum - 1].GetComponent<Rigidbody>();
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        m_onPlayerRespawn(playerNum);
    }

    private IEnumerator DeathAnimation(int playerNum)
    {
        Vector3 initial = m_players[playerNum - 1].transform.position;
        Vector3 final = initial + new Vector3(0, -m_deathAnimationDistance, 0);
        Vector3 pos;
        for (float t = 0; t < 1; t += Time.deltaTime / m_deathAnimationDuration)
        {
            yield return new WaitForFixedUpdate();
            pos = initial + (final - initial) * (Mathf.Sin(-(1.5f * Mathf.PI * t)) * t);
            m_players[playerNum-1].transform.position = pos;
        }
    }
    

    (int, int) AddPlayer()
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
                numPlayersPerTeam[playerTeamAssignment - 1]++;
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

            return (m_numPlayers, smallestTeamNum);
        }
        else
        {
            return (-1, -1);
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
                m_players[i].transform.localScale = spawnPos.transform.localScale;
                m_players[i].transform.rotation = spawnPos.transform.rotation;
                
                SwitchToActionMapForPlayer(i + 1, "InGame");
            }
        }
    }
}
