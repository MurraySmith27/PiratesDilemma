using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public delegate void DeleteBoatEvent(int teamNum, int boatNum);
public delegate void SpawnBoatEvent(int teamNum, int boatNum);
public delegate void GoldAddedToBoatEvent(int teamNum, int boatNum, int currentGold, int capacity);
public class BoatSystem : GameSystem
{
    private static BoatSystem _instance;
    
    public static BoatSystem Instance
    {
        get { return _instance; }
    }

    public DeleteBoatEvent m_onDeleteBoat;
    public SpawnBoatEvent m_onSpawnBoat;
    public GoldAddedToBoatEvent m_onGoldAddedToBoat;

    [SerializeField] private AudioSource m_boatSinkSound;
    [SerializeField] private AudioSource m_boatSailSound;
    
    [SerializeField] private float m_boatRespawnTime = 5f;

    [SerializeField] private int m_boatMinTimeToLive;
    [SerializeField] private int m_boatMaxTimeToLive;

    [SerializeField] private int m_boatMinCapacity;
    [SerializeField] private int m_boatMaxCapacity;

    [SerializeField] private List<GameObject> m_boatPrefabsPerTeam;
    
    public int m_numBoats
    {
        get { return m_numBoatsPerTeam.Sum(); }
    }

    [HideInInspector] public List<int> m_numBoatsPerTeam;


    private List<GameObject> m_boats
    {
        get
        {
            IEnumerable<GameObject> x = 
            from inner in m_boatsPerTeam
            from value in inner
            select value;
            return x.ToList();
        }
    }

    public List<List<GameObject>> m_boatsPerTeam;

    private List<List<Transform>> m_boatSpawnLocationsPerTeam;
    
    protected override void Start()
    {
    
        m_boatSpawnLocationsPerTeam = new List<List<Transform>>();
        m_boatsPerTeam = new List<List<GameObject>>();
        
        for (int i = 0; i < PlayerSystem.Instance.m_numTeams; i++)
        {
            m_boatSpawnLocationsPerTeam.Add(null);
        }
        
        //get spawn positions from gameobjects in scene with special tags.
        SetBoatSpawnPositions();
    
        for (int teamNum = 1; teamNum <= PlayerSystem.Instance.m_numTeams; teamNum++)
        {
            m_numBoatsPerTeam.Add(m_boatSpawnLocationsPerTeam[teamNum - 1].Count);
            m_boatsPerTeam.Add(new List<GameObject>());
            
            //Spawn boats
            for (int boatNum = 1; boatNum <= m_numBoatsPerTeam[teamNum - 1]; boatNum++)
            {
                m_boatsPerTeam[teamNum - 1].Add(null);
                SpawnBoat(teamNum, boatNum);
            }
        }
        
        base.Start();   
    }

    // Spawn the boats with their randomized timeToLive and boatTotalCapacity
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }
    
    private void SetBoatSpawnPositions()
    {

        for (int i = 0; i < PlayerSystem.Instance.m_numTeams; i++)
        {
            m_boatSpawnLocationsPerTeam[i] = new List<Transform>();
         
            foreach (GameObject spawnObjects in GameObject.FindGameObjectsWithTag($"Team{i+1}BoatSpawn"))
            {
                m_boatSpawnLocationsPerTeam[i].Add(spawnObjects.transform);
            }
        }
    }

    //Spawn a single boat for team teamNum, in that team's "boatNum"th spawn position.
    public void SpawnBoat(int teamNum, int boatNum)
    {
        
        GameObject newBoat = Instantiate(m_boatPrefabsPerTeam[teamNum-1], 
            m_boatSpawnLocationsPerTeam[teamNum - 1][boatNum - 1].transform.position, 
            m_boatSpawnLocationsPerTeam[teamNum - 1][boatNum - 1].transform.rotation);
        newBoat.transform.localScale = m_boatSpawnLocationsPerTeam[teamNum - 1][boatNum - 1].transform.localScale;
        
        
        m_boatsPerTeam[teamNum - 1][boatNum - 1] = newBoat;

    
            
        BoatData boatData = newBoat.GetComponent<BoatData>();
        boatData.m_teamNum = teamNum;
        boatData.m_boatNum = boatNum;
        boatData.m_timeToLive = new System.Random().Next(m_boatMinTimeToLive, m_boatMaxTimeToLive);
        boatData.m_goldCapacity = new System.Random().Next(m_boatMinCapacity, m_boatMaxCapacity);
        
        BoatGoldController boatGoldController = newBoat.GetComponent<BoatGoldController>();
        boatGoldController.m_onBoatSink += OnBoatSink;
        //here we inject our event callback into each instance of BoatGoldController.
        boatGoldController.m_onGoldAddedToBoat = OnGoldAddedToBoat;
        //the first child of the boat spawn position is the gold drop zone spawn position.
        Transform goldDropZoneSpawn = m_boatSpawnLocationsPerTeam[teamNum - 1][boatNum - 1].GetChild(0).transform;
        boatGoldController.m_goldDropZone.transform.position = goldDropZoneSpawn.position;
        boatGoldController.m_goldDropZone.transform.localScale = goldDropZoneSpawn.localScale;
        boatGoldController.m_onBoatSail += OnBoatSail;
    }
    
    private void OnGoldAddedToBoat(int teamNum, int boatNum, int goldTotal, int capacity)
    {
        m_onGoldAddedToBoat(teamNum, boatNum, goldTotal, capacity);
    }
    
    private void OnBoatSink(int teamNum, int boatNum)
    {
        StartCoroutine(SinkBoat(teamNum, boatNum));
    }
    
    private IEnumerator SinkBoat(int teamNum, int boatNum)
    {
        m_onDeleteBoat(teamNum, boatNum);
        m_boatsPerTeam[teamNum - 1][boatNum - 1].GetComponent<BoatData>().m_sinkAudioSource.Play();
        Coroutine sinkAnimationCoroutine = StartCoroutine(SinkBoatAnimation(teamNum, boatNum));
        
        foreach (Transform boardedPosition in m_boatsPerTeam[teamNum - 1][boatNum - 1].GetComponent<BoatData>()
                     .m_playerBoardedPositions)
        {
            if (boardedPosition.childCount > 0)
            {
                GameObject player = boardedPosition.GetChild(0).gameObject;
                player.transform.parent =
                    PlayerSystem.Instance.m_playersParents[player.GetComponent<PlayerData>().m_playerNum - 1];
                PlayerSystem.Instance.OnPlayerDie(player.GetComponent<PlayerData>().m_playerNum);
            }
        }

        yield return new WaitForSeconds(m_boatRespawnTime);
        StopCoroutine(sinkAnimationCoroutine);
        
        Destroy(m_boatsPerTeam[teamNum - 1][boatNum - 1]);
        
        SpawnBoat(teamNum, boatNum);
        m_onSpawnBoat(teamNum, boatNum);
    }
    
    IEnumerator SinkBoatAnimation(int teamNum, int boatNum)
    {
        
        while (true)
        {
            m_boatsPerTeam[teamNum - 1][boatNum - 1].transform.Translate(new Vector3(0, -50, 0) * Time.deltaTime);
            yield return null;
        }
    }

    private void OnBoatSail(int teamNum, int boatNum, List<int> goldScoredPerTeam)
    {
        StartCoroutine(SailBoat(teamNum, boatNum, goldScoredPerTeam));
    }
    
    IEnumerator SailBoat(int teamNum, int boatNum, List<int> goldScoredPerTeam)
    {
        m_onDeleteBoat(teamNum, boatNum);
        m_boatsPerTeam[teamNum - 1][boatNum - 1].GetComponent<BoatData>().m_sailAudioSource.Play();
        Coroutine sailAnimationCoroutine = StartCoroutine(SailBoatAnimation(teamNum, boatNum));
        ScoreSystem.Instance.UpdateScore(goldScoredPerTeam);

        yield return new WaitForSeconds(m_boatRespawnTime);
        StopCoroutine(sailAnimationCoroutine);
        
        
        Destroy(m_boatsPerTeam[teamNum - 1][boatNum - 1]);
        
        //respawn players that were on boat.
        foreach (Transform boardedPosition in m_boatsPerTeam[teamNum - 1][boatNum - 1].GetComponent<BoatData>()
                     .m_playerBoardedPositions)
        {
            GameObject player = boardedPosition.GetChild(0).gameObject;
            player.transform.parent = PlayerSystem.Instance.m_playersParents[player.GetComponent<PlayerData>().m_playerNum - 1];
            PlayerSystem.Instance.OnPlayerDie(player.GetComponent<PlayerData>().m_playerNum);
        }
        
        SpawnBoat(teamNum, boatNum);
        m_onSpawnBoat(teamNum, boatNum);
    }

    IEnumerator SailBoatAnimation(int teamNum, int boatNum)
    {
        while (true)
        {
            m_boatsPerTeam[teamNum - 1][boatNum - 1].transform.Translate(new Vector3(0, 0, 10) * Time.deltaTime);
            yield return null;
        }
    }
    
}
