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
public delegate void PlayerJoinEvent(int newPlayerNum);

public delegate void PlayerReadyUpToggleEvent(int playerNum, bool isReady);

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
    
    public List<GameObject> m_team1playerPrefabs;
    
    public List<GameObject> m_team2playerPrefabs;
    
    public int m_numTeams = 2;

    [HideInInspector] public int m_numPlayers = 0;

    [SerializeField] private float m_deathAnimationDistance = 5f;

    [SerializeField] private float m_deathAnimationDuration = 2f;

    [SerializeField] private float m_playerAirbornOnKrakenArrivalDuration = 1f;
    
    private List<bool> m_isPlayerDying;

    //used in character select screen to determine which players have readied up.
    public List<bool> m_readyPlayers;
    
    [HideInInspector]
    public List<PlayerInput> m_playerInputObjects
    {
        get
        {
            return m_players.Select(player => player.GetComponent<PlayerInput>()).ToList();
        }
    }

    [HideInInspector] public List<GameObject> m_players;

    [HideInInspector] public List<Transform> m_playersParents;

    [HideInInspector] public List<Transform> m_playerSpawnPositions { get; private set; }

    [SerializeField] private float m_playerRespawnTime = 3f;

    private List<PlayerControlSchemes> m_playerControlSchemesList;

    private Dictionary<int, InputDevice> m_assignedPlayerDevices;
    
    [HideInInspector] public List<int> m_playerTeamAssignments;
    
    public PlayerDieEvent m_onPlayerDie;

    public PlayerRespawnEvent m_onPlayerRespawn;
    
    public PlayerJoinEvent m_onPlayerJoin;

    public PlayerReadyUpToggleEvent m_onPlayerReadyUpToggle;

    public InputActionAsset m_actions;
    
    [SerializeField] private float m_startGameCountdownSeconds = 3f;
    
    [SerializeField] private string m_gameSceneToLoadName;

    private Coroutine m_startGameCountdownCoroutine;

    private List<GameObject> m_visualStandIns;
    
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

        m_playerControlSchemesList = new List<PlayerControlSchemes>();
        m_assignedPlayerDevices = new Dictionary<int, InputDevice>();

        for (int i = 0; i < m_maxNumPlayers; i++)
        {
            m_playerControlSchemesList.Add(new PlayerControlSchemes());
        }
        
        m_playerControlSchemesList[0].FindAction("Join").performed += OnJoinButtonPressed;
        m_playerControlSchemesList[0].FindAction("Join").Enable();
        
        m_players = new List<GameObject>();
        m_playersParents = new List<Transform>();
        m_playerTeamAssignments = new List<int>();
        m_isPlayerDying = new List<bool>();
        m_readyPlayers = new List<bool>();
        m_visualStandIns = new List<GameObject>();
        
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

    private void OnKrakenEmerge()
    {
        //need to update player spawn positions, then throw players to those positions
        for (int i = 0; i < m_numPlayers; i++)
        {
            GameObject.FindGameObjectWithTag($"P{i + 1}Spawn").transform.position = GameObject.FindGameObjectWithTag($"P{i+1}SpawnAfterKraken").transform.position;
            SetPlayerSpawnPositions();
        }
        
        StartCoroutine(ThrowPlayersToSpawnPositions());
    }
    
    private IEnumerator ThrowPlayersToSpawnPositions()
    {
        
        //disable components of player objects briefly
        foreach (GameObject player in m_players)
        {
            player.GetComponent<PlayerMovementController>().enabled = false;
            player.GetComponent<PlayerGoldController>().enabled = false;
        }
         
        List<Vector3> initialPlayerPositions = new List<Vector3>();
        List<Vector3> finalPlayerPositions = new List<Vector3>();
        List<float> playerTravelDistances = new List<float>();
        List<float> playerHeightDeltasWithFloor = new List<float>();
        List<Rigidbody> playerRbs = new List<Rigidbody>();
        
        for (int i = 0; i < PlayerSystem.Instance.m_numPlayers; i++)
        {
            initialPlayerPositions.Add(PlayerSystem.Instance.m_players[i].transform.position);
            finalPlayerPositions.Add(PlayerSystem.Instance.m_playerSpawnPositions[i].transform.position);
            playerTravelDistances.Add((finalPlayerPositions[i] - initialPlayerPositions[i]).magnitude);
            playerRbs.Add(m_players[i].GetComponent<Rigidbody>());

            playerHeightDeltasWithFloor.Add(0.5f);
            
            RaycastHit hit;

            if (Physics.Raycast(initialPlayerPositions[i], new Vector3(0, -1, 0), maxDistance: 0f, hitInfo: out hit,
                    layerMask: ~~LayerMask.GetMask(new string[]{"Floor"})))
            {
                playerHeightDeltasWithFloor[i] = hit.distance;
            }
        }
        
        for (float t = 0; t < 1; t += Time.fixedDeltaTime / m_playerAirbornOnKrakenArrivalDuration)
        {
            for (int i = 0; i < m_numPlayers; i++)
            {
                Vector3 newPos = initialPlayerPositions[i] + (finalPlayerPositions[i] - initialPlayerPositions[i]) * t;

                float distance = playerTravelDistances[i];
                if (distance == 0)
                {
                    continue;
                }

                newPos.y = -(t * distance - distance) *
                           (t * distance + (playerHeightDeltasWithFloor[i] / distance));

                playerRbs[i].MovePosition(newPos);
            }
            yield return new WaitForFixedUpdate();
        }
        
        //re-enable components of player objects briefly
        foreach (GameObject player in m_players)
        {
            player.GetComponent<PlayerMovementController>().enabled = true;
            player.GetComponent<PlayerGoldController>().enabled = true;
        }
    }


    public void OnJoinButtonPressed(InputAction.CallbackContext ctx)
    {
        foreach (InputDevice device in m_assignedPlayerDevices.Values)
        {
            if (device.deviceId == ctx.control.device.deviceId)
            {
                Debug.Log("already connected with this device. Returning early");
                return;
            }
        }
        
        int playerNum, teamNum;
        (playerNum, teamNum) = this.AddPlayer();
        if (playerNum == -1)
        {
            //at player limit. destroy.
            Destroy(this.gameObject);
        }

        List<int> numPlayersPerTeam = new List<int>();
        for (int i = 0; i < m_numTeams; i++)
        {
            numPlayersPerTeam.Add(0);
        }

        foreach (int playerTeamAssignment in m_playerTeamAssignments)
        {
            numPlayersPerTeam[playerTeamAssignment - 1]++;
        }

        GameObject playerPrefab;
        if (teamNum == 1)
        {
            playerPrefab = m_team1playerPrefabs[numPlayersPerTeam[teamNum - 1] - 1];
        }
        else
        {
            playerPrefab = m_team2playerPrefabs[numPlayersPerTeam[teamNum - 1] - 1];
        }


        GameObject newPlayerInstance = Instantiate(playerPrefab,
            m_playerSpawnPositions[playerNum - 1].position, Quaternion.identity);
        
        newPlayerInstance.GetComponentInChildren<PlayerMovementController>().m_onPlayerDie += OnPlayerDie;

        PlayerData playerData = newPlayerInstance.GetComponentInChildren<PlayerData>();

        playerData.m_playerNum = playerNum;
        playerData.m_teamNum = m_playerTeamAssignments[playerNum - 1];

        m_players.Add(playerData.gameObject);
        m_players[playerNum - 1].transform.localScale = m_playerSpawnPositions[playerNum - 1].localScale;
        m_players[playerNum - 1].transform.rotation = m_playerSpawnPositions[playerNum - 1].rotation;

        PlayerInput playerInput = newPlayerInstance.GetComponentInChildren<PlayerInput>();
        
        RegisterDeviceWithPlayer(playerNum, playerInput, ctx.control.device);

        if (playerNum < m_maxNumPlayers)
        {
            //add the callback for the next player.
            m_playerControlSchemesList[playerNum].FindAction("Join").performed += OnJoinButtonPressed;
            m_playerControlSchemesList[playerNum].FindAction("Join").Enable();
        }
        
        playerInput.actions.FindAction("Join").performed -= OnJoinButtonPressed;
        playerInput.actions.FindAction("Join").Disable();
        
        //after player joins, we also enable the in-game action map so they can test out the controls and ready up
        SwitchToActionMapForPlayer(playerNum, "InGame");
        
        playerInput.actions.FindAction("ReadyUp").performed += (ctx => OnReadyUpButtonPressed(playerNum));
        
        m_playersParents.Add(newPlayerInstance.transform);
        
        //make the purely visual version of the mesh visible and move it to the right location.
        for (int i = 0; i < newPlayerInstance.transform.childCount; i++)
        {
            Transform child = newPlayerInstance.transform.GetChild(i);
            if (child.CompareTag("VisualStandIn"))
            {
                m_visualStandIns.Add(child.gameObject);
                child.gameObject.SetActive(true);
                child.gameObject.transform.position =
                    GameObject.FindGameObjectWithTag($"P{playerNum}CharacterSelectSpawn").transform.position;
            }
        }
        
        //then start game for player scripts so we can test out movement in character select.
        m_players[playerNum - 1].GetComponent<PlayerMovementController>().OnGameStart();
        m_players[playerNum - 1].GetComponent<PlayerGoldController>().OnGameStart();
        
        m_readyPlayers.Add(false);
        
        DontDestroyOnLoad(newPlayerInstance);
        m_onPlayerJoin(playerNum);
    }

    void OnReadyUpButtonPressed(int playerNum)
    {

        Debug.Log($"on ready up for player: {playerNum}");
        m_readyPlayers[playerNum - 1] = !m_readyPlayers[playerNum - 1];
        
        bool startGame = true;
        foreach (bool isPlayerReady in m_readyPlayers)
        {
            if (!isPlayerReady)
            {
                startGame = false;
                break;
            }
        }

        if (startGame)
        {
            m_startGameCountdownCoroutine = StartCoroutine(StartGameCountdown());
        }
        else
        {
            if (m_startGameCountdownCoroutine != null)
            {
                StopCoroutine(m_startGameCountdownCoroutine);
                m_startGameCountdownCoroutine = null;
            }
        }

        m_onPlayerReadyUpToggle(playerNum, m_readyPlayers[playerNum - 1]);
    }
    
    private IEnumerator StartGameCountdown()
    {
        yield return new WaitForSeconds(m_startGameCountdownSeconds);
        SceneManager.LoadScene(m_gameSceneToLoadName);
    }
    
    public void OnPlayerDie(int playerNum)
    {
        if (!m_isPlayerDying[playerNum - 1])
        {
            StartCoroutine(WaitForRespawn(playerNum));
            if (m_onPlayerDie != null && m_onPlayerDie.GetInvocationList().Length > 0)
            {
                m_onPlayerDie(playerNum);
            }
        }
    }

    private IEnumerator WaitForRespawn(int playerNum)
    {
        m_isPlayerDying[playerNum - 1] = true;
        m_players[playerNum - 1].GetComponent<PlayerMovementController>().enabled = false;
        m_players[playerNum - 1].GetComponent<Collider>().enabled = false;
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
        m_players[playerNum - 1].GetComponent<Collider>().enabled = true;

        m_players[playerNum - 1].transform.position = m_playerSpawnPositions[playerNum - 1].position;

        Rigidbody rb = m_players[playerNum - 1].GetComponent<Rigidbody>();
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (m_onPlayerRespawn != null && m_onPlayerRespawn.GetInvocationList().Length > 0)
        {
            m_onPlayerRespawn(playerNum);
        }

        m_isPlayerDying[playerNum - 1] = false;
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
            
            m_isPlayerDying.Add(false);

            return (m_numPlayers, smallestTeamNum);
        }
        else
        {
            return (-1, -1);
        }
    }

    public void RegisterDeviceWithPlayer(int playerNum, PlayerInput playerInput, InputDevice device)
    {
        if (playerNum > m_numPlayers)
        {
            throw new ArgumentException($"Cannot change device for player number {playerNum}. " +
                                        $"There are only {m_numPlayers} players registered.");
        }
        string controlScheme = "Keyboard";
        if (device is Gamepad)
        {
            controlScheme = "Gamepad";
        }

        m_playerControlSchemesList[playerNum - 1].devices = new[] { device };
        
        playerInput.SwitchCurrentControlScheme(controlScheme, new[] { device });
        
        m_assignedPlayerDevices.Add(playerNum, device);
    }

    public void DeregisterDeviceFromPlayer(int playerNum, PlayerInput playerInput, InputDevice device)
    {
        if (playerNum > m_numPlayers)
        {
            throw new ArgumentException($"Cannot change device for player number {playerNum}. " +
                                        $"There are only {m_numPlayers} players registered.");
        }

        m_playerControlSchemesList[playerNum - 1].devices = null;
        
        string controlScheme = "Keyboard";
        if (device is Gamepad)
        {
            controlScheme = "Gamepad";
        }
        
        playerInput.SwitchCurrentControlScheme(controlScheme, null);
    }

    public void SwitchToActionMapForPlayer(int playerNum, string actionMapName)
    {
        m_playerControlSchemesList[playerNum - 1].CharacterSelect.Disable();
        m_playerInputObjects[playerNum - 1].actions.Enable();
        
        m_playerInputObjects[playerNum - 1].actions.FindActionMap(actionMapName).Enable();
    }

    public void OnEnable()
    {
        
        foreach (PlayerInput playerInput in m_playerInputObjects)
        {
            playerInput.actions.Enable();
        }
        
        m_actions.FindActionMap("CharacterSelect").Enable();
    }
    
    public void OnDisable()
    {
        foreach (PlayerInput playerInput in m_playerInputObjects)
        {
            playerInput.actions.Disable();
        }
        
        m_actions.FindActionMap("CharacterSelect").Disable();
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
                
                m_visualStandIns[i].SetActive(false);
                
                SwitchToActionMapForPlayer(i + 1, "InGame");
            }

            if (m_numPlayers < m_maxNumPlayers)
            {
                m_playerControlSchemesList[m_numPlayers].CharacterSelect.Disable();
            }
        }

        KrakenController.Instance.m_onKrakenEmerge += OnKrakenEmerge;
    }
}
