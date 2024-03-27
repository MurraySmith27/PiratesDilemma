using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

[System.Serializable]
public class TeamData
{
    public Color color;
    public string name;
    public Material mainMaterial;
    public Material accentMaterial;
}


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

    public List<TeamData> m_team1DataOptions; //this stores all options for team data.
    
    public List<TeamData> m_team2DataOptions;
    
    [HideInInspector] public List<TeamData> m_teamData; //this stores the currently selected ones.

    private int m_team1DataOptionIndex;
    
    private int m_team2DataOptionIndex;
    
    public List<GameObject> m_team1playerPrefabs;
    
    public List<GameObject> m_team2playerPrefabs;
    
    public int m_numTeams = 2;

    [HideInInspector] public int m_numPlayers = 0;

    [HideInInspector]
    public List<int> m_numPlayersPerTeam
    {
        get
        {
            List<int> arr = new();

            for (int i = 0; i < m_numTeams; i++)
            {
                arr.Add(0);
            }

            foreach (int assignment in m_playerTeamAssignments)
            {
                arr[assignment - 1]++;
            }

            return arr;
        }
        private set { }
    }
    
    //maps the player number to the player number within that team
    public List<int> m_playerNumToTeamPlayerNum
    {
        get
        {
            List<int> arr = new();

            List<int> numPlayersSoFarPerTeam = new();
            for (int i = 0; i < m_numTeams; i++)
            {
                numPlayersSoFarPerTeam.Add(0);
            }
            for (int i = 0; i < m_playerTeamAssignments.Count; i++)
            {
                arr.Add(++numPlayersSoFarPerTeam[m_playerTeamAssignments[i]-1]);
            }

            return arr;
        }
        private set {}
    }
    
    //maps the team-specific player num to the global player num, for each team
    public List<List<int>> m_teamPlayerNumToPlayerNum
    {
        get
        {
            List<List<int>> arr = new();

            for (int i = 0; i < m_numTeams; i++)
            {
                List<int> teamToPlayerNumForThisTeam = new();
                int teamNum = i + 1;
                for (int j = 0; j < m_playerTeamAssignments.Count; j++)
                {
                    if (m_playerTeamAssignments[j] == teamNum)
                    {
                        teamToPlayerNumForThisTeam.Add(j+1);
                    }
                }
                arr.Add(teamToPlayerNumForThisTeam);
            }

            return arr;
        }
        private set {}
    }

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

    //this is public so we can simulate inputs.
    public List<PlayerControlSchemes> m_playerControlSchemesList;

    private Dictionary<int, InputDevice> m_assignedPlayerDevices;
    
    [HideInInspector] public List<int> m_playerTeamAssignments;
    
    public PlayerDieEvent m_onPlayerDie;

    public PlayerRespawnEvent m_onPlayerRespawn;
    
    public PlayerJoinEvent m_onPlayerJoin;

    public PlayerReadyUpToggleEvent m_onPlayerReadyUpToggle;

    public PlayerPickUpBombEvent m_onPlayerPickupBomb;

    public PlayerDropBombEvent m_onPlayerDropBomb;
    
    public PlayerDashCooldownStartEvent m_onDashCooldownStart;

    public InputActionAsset m_actions;
    
    [SerializeField] private float m_startGameCountdownSeconds = 3f;
    
    [SerializeField] private float m_leavePreviousSceneBufferTime = 0.5f;
    
    [SerializeField] private List<string> m_gameScenesToLoadNames;

    private int m_numLevelsPlayed = 0;

    private Coroutine m_startGameCountdownCoroutine;

    public List<GameObject> m_visualStandIns;

    private bool m_debugMode = false;
    
    private void Awake()
    {
        if (PlayerSystem._instance != null && PlayerSystem._instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            PlayerSystem._instance = this;
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

            m_teamData = new List<TeamData>();
            m_team1DataOptionIndex = 0;
            m_teamData.Add(m_team1DataOptions[0]);
            m_team2DataOptionIndex = 0;
            m_teamData.Add(m_team2DataOptions[0]);

            m_numLevelsPlayed = 0;
        
            DontDestroyOnLoad(this.gameObject);    
        }
    }

    void Start()
    {
        SetPlayerSpawnPositions();
        SceneManager.sceneLoaded += OnGameSceneLoaded;
        SceneManager.sceneUnloaded += OnGameSceneUnloaded;

        base.SystemReady();
    }
    
    //TODO: REmove this placeholder code:
    void Update()
    {
        if (Input.GetKeyDown("u"))
        {
            m_numLevelsPlayed++;
        }

        if (Input.GetKeyDown("i"))
        {
            TestWithFourPlayers();
        }
    }

    private void TestWithFourPlayers() {
        m_debugMode = true;
        for (int i = m_numPlayers; i < m_maxNumPlayers; i++)
        {
            OnJoinButtonPressed(new InputAction.CallbackContext());
        }

        for (int i = 0; i < m_maxNumPlayers; i++)
        {
            OnReadyUpButtonPressed(i + 1);
        }
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

    public void LockPlayer(int playerNum)
    {
        m_players[playerNum-1].GetComponent<PlayerMovementController>().enabled = false;
        m_players[playerNum-1].GetComponent<PlayerItemController>().enabled = false;
    }

    public void UnlockPlayer(int playerNum)
    {
        m_players[playerNum-1].GetComponent<PlayerMovementController>().enabled = true;
        m_players[playerNum-1].GetComponent<PlayerItemController>().enabled = true;
    }
    
    private IEnumerator ThrowPlayersToSpawnPositions()
    {
        
        //disable components of player objects briefly
        for (int i = 1; i <= m_numPlayers; i++)
        {
            LockPlayer(i);
        }
         
        List<Vector3> initialPlayerPositions = new List<Vector3>();
        List<Vector3> finalPlayerPositions = new List<Vector3>();
        List<float> playerTravelDistances = new List<float>();
        List<float> playerHeightDeltasWithFloor = new List<float>();
        List<CharacterController> playerCharacterControllers = new List<CharacterController>();
        
        for (int i = 0; i < PlayerSystem.Instance.m_numPlayers; i++)
        {
            initialPlayerPositions.Add(PlayerSystem.Instance.m_players[i].transform.position);
            finalPlayerPositions.Add(PlayerSystem.Instance.m_playerSpawnPositions[i].transform.position);
            playerTravelDistances.Add((new Vector3(finalPlayerPositions[i].x, 0, finalPlayerPositions[i].z) - new Vector3(initialPlayerPositions[i].x, 0, initialPlayerPositions[i].z)).magnitude);
            playerCharacterControllers.Add(m_players[i].GetComponent<CharacterController>());

            playerHeightDeltasWithFloor.Add(m_players[i].transform.position.y - 
                                            m_players[i].GetComponent<PlayerMovementController>().m_feetPosition.transform.position.y);
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

                playerCharacterControllers[i].Move(newPos - m_players[i].transform.position);
            }
            yield return new WaitForFixedUpdate();
        }
        
        //re-enable components of player objects briefly
        for (int i = 1; i <= m_numPlayers; i++)
        {
            UnlockPlayer(i);
        }
        
    }
    
    public void OnJoinButtonPressed(InputAction.CallbackContext ctx)
    {
        if (!m_debugMode)
        {
            foreach (InputDevice device in m_assignedPlayerDevices.Values)
            {
                if (device.deviceId == ctx.control.device.deviceId)
                {
                    //already connected to annother player on this device. Return early.
                    return;
                }
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

        if (!m_debugMode)
        {
            RegisterDeviceWithPlayer(playerNum, playerInput, ctx.control.device);
        }

        if (playerNum < m_maxNumPlayers)
        {
            //add the callback for the next player.
            m_playerControlSchemesList[playerNum].FindAction("Join").performed += OnJoinButtonPressed;
            m_playerControlSchemesList[playerNum].FindAction("Join").Enable();
        }
        
        
        m_playerControlSchemesList[playerNum-1].FindAction("Join").performed -= OnJoinButtonPressed;
        m_playerControlSchemesList[playerNum-1].FindAction("Join").Disable();
        
        playerInput.actions.FindAction("Join").performed -= OnJoinButtonPressed;
        playerInput.actions.FindAction("Join").Disable();

        playerInput.actions.FindAction("ChangeTeamColor").performed += (ctx => OnChangeTeamColorActionPerformed(teamNum));
        playerInput.actions.FindAction("ChangeTeamColor").Enable();
        
        //after player joins, we also enable the in-game action map so they can test out the controls and ready up
        SwitchToActionMapForPlayer(playerNum, "InGame");

        if (!m_debugMode)
        {
            playerInput.actions.FindAction("ReadyUp").performed += (ctx => OnReadyUpButtonPressed(playerNum));
        }
        
        m_playersParents.Add(newPlayerInstance.transform);
        
        //make the purely visual version of the mesh visible and move it to the right location.
        for (int i = 0; i < newPlayerInstance.transform.childCount; i++)
        {
            Transform child = newPlayerInstance.transform.GetChild(i);
            if (child.CompareTag("VisualStandIn"))
            {
                m_visualStandIns.Add(child.gameObject);
                child.gameObject.SetActive(true);
                GameObject characterSelectSpawn = GameObject.FindGameObjectWithTag($"P{playerNum}CharacterSelectSpawn"); 
                child.gameObject.transform.position = characterSelectSpawn.transform.position;
                child.gameObject.transform.rotation = characterSelectSpawn.transform.rotation;
                child.gameObject.transform.localScale = characterSelectSpawn.transform.localScale;
            }
        }
        
        //then start game for player scripts so we can test out movement in character select.
        PlayerMovementController playerMovementController = m_players[playerNum - 1].GetComponent<PlayerMovementController>();

        playerMovementController.m_onDashCooldownStart += OnPlayerDashCooldownStart;
        
        playerMovementController.OnGameStart();

        PlayerItemController playerItemController = m_players[playerNum - 1].GetComponent<PlayerItemController>();
        
        playerItemController.m_onPlayerPickupBomb += OnPlayerPickUpBomb;
        playerItemController.m_onPlayerDropBomb += OnPlayerDropBomb;
        
        playerItemController.OnGameStart();
        
        PlayerAnimationController playerAnimationController = m_players[playerNum - 1].GetComponent<PlayerAnimationController>();
        
        playerAnimationController.OnGameStart();
        
        playerAnimationController.SetTeamMaterials(m_teamData[teamNum-1].mainMaterial, m_teamData[teamNum-1].accentMaterial);
        
        m_readyPlayers.Add(false);
        
        DontDestroyOnLoad(newPlayerInstance);
        m_onPlayerJoin(playerNum);
    }

    private void OnChangeTeamColorActionPerformed(int teamNum) 
    {

        if (teamNum == 1)
        {
            m_team1DataOptionIndex++;
            m_teamData[0] = m_team1DataOptions[m_team1DataOptionIndex % m_team1DataOptions.Count];
        }
        else
        {
            m_team2DataOptionIndex++;
            m_teamData[1] = m_team2DataOptions[m_team2DataOptionIndex % m_team2DataOptions.Count];
        }


        foreach (GameObject player in m_players)
        {
            if (player.GetComponent<PlayerData>().m_teamNum == teamNum)
            {
                PlayerAnimationController playerAnimationController = player.GetComponent<PlayerAnimationController>();
                
                playerAnimationController.SetTeamMaterials(m_teamData[teamNum-1].mainMaterial, m_teamData[teamNum-1].accentMaterial);
            }
        }
        
    }

    public void OnReadyUpButtonPressed(int playerNum)
    {
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
        if (m_numPlayers < m_maxNumPlayers)
        {
            m_playerControlSchemesList[m_numPlayers].FindAction("Join").performed -= OnJoinButtonPressed;
            m_playerControlSchemesList[m_numPlayers].FindAction("Join").Disable();
        }
        PlayerInput playerInput = m_players[m_numPlayers - 1].GetComponent<PlayerInput>();
        playerInput.actions.FindAction("Join").performed -= OnJoinButtonPressed;
        playerInput.actions.FindAction("Join").Disable();
        for (int i = 0; i < m_numPlayers; i++)
        {
            m_players[i].GetComponent<PlayerInput>().actions.FindAction("ReadyUp").Disable();
        }

        GameTimerSystem.Instance.StopGame(m_gameScenesToLoadNames[m_numLevelsPlayed % m_gameScenesToLoadNames.Count]);
        m_numLevelsPlayed++;
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
        
        PlayerItemController playerItemController = m_players[playerNum - 1].GetComponent<PlayerItemController>();
        LockPlayer(playerNum);
        if (m_players[playerNum-1].GetComponent<PlayerData>().m_bombsCarried > 0)
        {
            playerItemController.DropAllBombs(false);
        }

        Coroutine deathCoroutine = StartCoroutine(DeathAnimation(playerNum));
     
        yield return new WaitForSeconds(m_playerRespawnTime);
        //if still running, kill it
        StopCoroutine(deathCoroutine);
        
        UnlockPlayer(playerNum);
            
        PlayerMovementController playerMovementController = 
        m_players[playerNum - 1].GetComponent<PlayerMovementController>();
        playerMovementController.WarpToPosition(m_playerSpawnPositions[playerNum - 1].position);
        playerMovementController.MakeInvulnerable();

        PlayerAnimationController playerAnimationController =
            m_players[playerNum - 1].GetComponent<PlayerAnimationController>();

        playerAnimationController.SetInvulnerableMaterial();
        playerAnimationController.OnRespawn();
        
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
        PlayerMovementController playerMovementController =
            m_players[playerNum - 1].GetComponent<PlayerMovementController>();
        for (float t = 0; t < 1; t += Time.deltaTime / m_deathAnimationDuration)
        {
            yield return new WaitForFixedUpdate();
            pos = initial + (final - initial) * (Mathf.Sin(-(1.5f * Mathf.PI * t)) * t);
            playerMovementController.WarpToPosition(pos);;
        }
    }

    private void OnPlayerPickUpBomb(int teamNum, int playerNum)
    {
        if (m_onPlayerPickupBomb != null && m_onPlayerPickupBomb.GetInvocationList().Length > 0)
        {
            m_onPlayerPickupBomb(teamNum, playerNum);
        }
    }

    private void OnPlayerDropBomb(int teamNum, int playerNum)
    {
        if (m_onPlayerDropBomb != null && m_onPlayerDropBomb.GetInvocationList().Length > 0)
        {
            m_onPlayerDropBomb(teamNum, playerNum);
        }
    }

    private void OnPlayerDashCooldownStart(int teamNum, int playerNum, float cooldownSeconds)
    {
        if (m_onDashCooldownStart != null && m_onDashCooldownStart.GetInvocationList().Length > 0)
        {
            m_onDashCooldownStart(teamNum, playerNum, cooldownSeconds);
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
        m_playerInputObjects[playerNum - 1].actions.FindActionMap(actionMapName).Enable();
    }

    void OnEnable()
    {
        
        foreach (PlayerInput playerInput in m_playerInputObjects)
        {
            playerInput.actions.Enable();
        }
        
        m_actions.FindActionMap("CharacterSelect").Enable();

    }
    
    void OnDisable()
    {
        foreach (PlayerInput playerInput in m_playerInputObjects)
        {
            playerInput.actions.Disable();
        }
        
        m_actions.FindActionMap("CharacterSelect").Disable();
        
    }
    
    protected override void OnDestroy()
    {
        if (_instance == this)
        {
            base.OnDestroy();
            _instance = null;
        }
    }

    private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {

        //get spawn positions from gameobject in scene with special tags.
        SetPlayerSpawnPositions();

        bool isCharacterSelect = scene.name == GameTimerSystem.Instance.m_characterSelectSceneName;
        //move players to spawn positions
        for (int i = 0; i < m_numPlayers; i++)
        {
            GameObject spawnPos = GameObject.FindGameObjectWithTag($"P{i + 1}Spawn");
            PlayerInput playerInput = m_players[i].GetComponentInChildren<PlayerInput>();
            
            m_players[i].GetComponent<PlayerMovementController>().WarpToPosition(spawnPos.transform.position);
            m_players[i].transform.localScale = spawnPos.transform.localScale;
            m_players[i].transform.rotation = spawnPos.transform.rotation;
            m_playerControlSchemesList[i].FindAction("ReadyUp").Disable();
            m_playerControlSchemesList[i].FindAction("ReadyUp").performed -= (ctx => OnReadyUpButtonPressed(i+1));
            
            playerInput = m_players[i].GetComponentInChildren<PlayerInput>();
            playerInput.actions.FindAction("ReadyUp").Disable();
            playerInput.actions.FindAction("ReadyUp").performed -= (ctx => OnReadyUpButtonPressed(i+1));
            
            playerInput.actions.FindAction("ChangeTeamColor").Disable();

            
            PlayerItemController playerItemController = m_players[i].GetComponent<PlayerItemController>();

            playerItemController.m_onPlayerPickupBomb += OnPlayerPickUpBomb;
            playerItemController.m_onPlayerDropBomb += OnPlayerDropBomb;
            
            SwitchToActionMapForPlayer(i + 1, "InGame");
        }

        if (isCharacterSelect)
        {
            for (int i = 0; i < m_numPlayers; i++)
            {
                PlayerInput playerInput = m_players[i].GetComponentInChildren<PlayerInput>();
                GameObject spawnPos = GameObject.FindGameObjectWithTag($"P{i + 1}Spawn");

                m_players[i].GetComponent<PlayerMovementController>().WarpToPosition(spawnPos.transform.position);
                m_players[i].transform.localScale = spawnPos.transform.localScale;
                m_players[i].transform.rotation = spawnPos.transform.rotation;
                
                m_playerControlSchemesList[i].FindAction("ReadyUp").Enable();
                m_playerControlSchemesList[i].FindAction("Join").Disable();
                playerInput.actions.FindAction("Join").Disable();
                
                playerInput.actions.FindAction("ChangeTeamColor").Enable();
                
                m_players[i].GetComponent<PlayerMovementController>().OnGameStart();
                m_players[i].GetComponent<PlayerItemController>().OnGameStart();
                m_players[i].GetComponent<PlayerAnimationController>().OnGameStart();
                m_visualStandIns[i].SetActive(true);
                
                m_visualStandIns[i].GetComponent<Animator>().SetTrigger("Idle");
                
                GameObject characterSelectSpawn = GameObject.FindGameObjectWithTag($"P{i+1}CharacterSelectSpawn"); 
                m_visualStandIns[i].gameObject.transform.position = characterSelectSpawn.transform.position;
                m_visualStandIns[i].gameObject.transform.rotation = characterSelectSpawn.transform.rotation;
                m_visualStandIns[i].gameObject.transform.localScale = characterSelectSpawn.transform.localScale;

                m_readyPlayers[i] = false;
                
                m_onPlayerJoin(i+1);
            }
            
            
            if (m_numPlayers < m_maxNumPlayers)
            {
                //add the callback for the next player.
                // m_playerControlSchemesList[m_numPlayers].FindAction("Join").performed += OnJoinButtonPressed;
                // m_playerControlSchemesList[m_numPlayers].FindAction("Join").Enable();
            }
        }
        else if (GameTimerSystem.Instance.m_levelSceneNames.Contains(scene.name))
        {

            if (m_numPlayers < m_maxNumPlayers)
            {
                m_playerControlSchemesList[m_numPlayers].CharacterSelect.Disable();
                m_playerControlSchemesList[m_numPlayers].CharacterSelect.Disable();
            }
        }
        
        if (KrakenController.Instance != null) {
            KrakenController.Instance.m_onKrakenEmerge += OnKrakenEmerge;
        }
    }

    private void OnGameSceneUnloaded(Scene scene)
    {
        for (int i = 0; i < m_numPlayers; i++)
        {
            PlayerItemController playerItemController = m_players[i].GetComponent<PlayerItemController>();

            playerItemController.m_onPlayerPickupBomb -= OnPlayerPickUpBomb;
            playerItemController.m_onPlayerDropBomb -= OnPlayerDropBomb;
            
            m_visualStandIns[i].SetActive(false);
        }
    }
}
