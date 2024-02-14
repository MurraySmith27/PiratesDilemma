using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public delegate void DeleteBoatEvent(int teamNum, int boatNum);
public delegate void ResetBoatEvent(int teamNum, int boatNum);
public delegate void GoldAddedToBoatEvent(int teamNum, int boatNum, int currentGold, int capacity);
public class BoatSystem : GameSystem
{
    private static BoatSystem _instance;
    
    public static BoatSystem Instance
    {
        get { return _instance; }
    }

    public DeleteBoatEvent m_onDeleteBoat;
    public ResetBoatEvent m_onResetBoat;
    public GoldAddedToBoatEvent m_onGoldAddedToBoat;

    [SerializeField] private AudioSource m_boatSinkSound;
    [SerializeField] private AudioSource m_boatSailSound;
    
    [SerializeField] private float m_boatResetTime = 5f;
    [SerializeField] private float m_boatSailBackTime = 1f;

    [SerializeField] private int m_boatMinTimeToLive;
    [SerializeField] private int m_boatMaxTimeToLive;
    
    public int m_numBoats
    {
        get { return m_numBoatsPerTeam.Sum(); }
    }

    [HideInInspector] public List<int> m_numBoatsPerTeam;


    public List<GameObject> m_boats
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

    private List<List<Vector3>> m_boatInitialPositionsPerTeam;

    private List<List<Vector3>> m_boatGoldDropZoneInitialPositionsPerTeam;
    
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
    
    void Start()
    {
        m_boatInitialPositionsPerTeam = new List<List<Vector3>>();
        m_boatGoldDropZoneInitialPositionsPerTeam = new List<List<Vector3>>();
        m_boatsPerTeam = new List<List<GameObject>>();
        
        for (int i = 0; i < PlayerSystem.Instance.m_numTeams; i++)
        {
            GameObject[] teamBoats = GameObject.FindGameObjectsWithTag($"Team{i + 1}Boat");
            m_boatsPerTeam.Add(new List<GameObject>());
            
            m_numBoatsPerTeam.Add(teamBoats.Length);
            
            m_boatInitialPositionsPerTeam.Add(new List<Vector3>());
            m_boatGoldDropZoneInitialPositionsPerTeam.Add(new List<Vector3>());
            
            for (int j = 0; j < teamBoats.Length; j++)
            {
                GameObject boat = teamBoats[j].transform.GetChild(0).gameObject;
                boat.GetComponent<BoatData>().m_teamNum = i + 1;
                boat.GetComponent<BoatData>().m_boatNum = j + 1;
                
                
                //inject our callbacks
                BoatGoldController boatGoldController = boat.GetComponent<BoatGoldController>();
                boatGoldController.m_onBoatSink += OnBoatSink;
                boatGoldController.m_onGoldAddedToBoat = OnGoldAddedToBoat;
                boatGoldController.m_onBoatSail += OnBoatSail;
                
                m_boatsPerTeam[i].Add(boat);
                m_boatInitialPositionsPerTeam[i].Add(boat.transform.position);
                m_boatGoldDropZoneInitialPositionsPerTeam[i].Add(boatGoldController.m_goldDropZone.transform.position);
            }
        }
        
        base.SystemReady();
    }

    void ResetBoat(int teamNum, int boatNum, bool smoothReset)
    {
        StartCoroutine(ResetToInitialPosition(teamNum, boatNum, smoothReset));
            
        GameObject boat = m_boatsPerTeam[teamNum - 1][boatNum - 1];
        BoatData boatData = boat.GetComponent<BoatData>();
        boatData.ClearGoldStored();

        BoatGoldController boatGoldController = boat.GetComponent<BoatGoldController>();
        boatGoldController.m_acceptingGold = true;
    }

    private IEnumerator ResetToInitialPosition(int teamNum, int boatNum, bool smoothReset)
    {

        GameObject boat = m_boatsPerTeam[teamNum-1][boatNum-1];
        if (smoothReset)
        {
            Vector3 initialPos = boat.transform.position;

            Vector3 finalPos = m_boatInitialPositionsPerTeam[teamNum-1][boatNum-1];

            for (float t = 0; t < 1; t += Time.deltaTime / m_boatSailBackTime)
            {
                boat.transform.position = Vector3.Lerp(initialPos, finalPos, t);
                yield return null;
            }
        }
        else
        {
            boat.transform.position = m_boatInitialPositionsPerTeam[teamNum-1][boatNum-1];
        }

        GameObject goldDropZone = boat.GetComponent<BoatGoldController>().m_goldDropZone;
        goldDropZone.transform.position = m_boatGoldDropZoneInitialPositionsPerTeam[teamNum-1][boatNum-1];
    }

    
    // private void SetBoatSpawnPositions()
    // {
    //
    //     for (int i = 0; i < PlayerSystem.Instance.m_numTeams; i++)
    //     {
    //         m_boatSpawnLocationsPerTeam[i] = new List<Transform>();
    //      
    //         foreach (GameObject spawnObjects in GameObject.FindGameObjectsWithTag($"Team{i+1}BoatSpawn"))
    //         {
    //             m_boatSpawnLocationsPerTeam[i].Add(spawnObjects.transform);
    //         }
    //     }
    // }

    //Spawn a single boat for team teamNum, in that team's "boatNum"th spawn position.
    // public void SpawnBoat(int teamNum, int boatNum)
    // {
    //     
    //     // GameObject newBoat = Instantiate(m_boatPrefabsPerTeam[teamNum-1], 
    //     //     m_boatSpawnLocationsPerTeam[teamNum - 1][boatNum - 1].transform.position, 
    //     //     m_boatSpawnLocationsPerTeam[teamNum - 1][boatNum - 1].transform.rotation);
    //     // newBoat.transform.localScale = m_boatSpawnLocationsPerTeam[teamNum - 1][boatNum - 1].transform.localScale;
    //     //
    //     // m_boatsPerTeam[teamNum - 1][boatNum - 1] = newBoat;
    //         
    //     // BoatData boatData = newBoat.GetComponent<BoatData>();
    //     // boatData.m_teamNum = teamNum;
    //     // boatData.m_boatNum = boatNum;
    //     // boatData.m_timeToLive = new System.Random().Next(m_boatMinTimeToLive, m_boatMaxTimeToLive);
    //     // boatData.m_goldCapacity = m_boatCapacity;
    //     
    //     // BoatGoldController boatGoldController = newBoat.GetComponent<BoatGoldController>();
    //     // boatGoldController.m_onBoatSink += OnBoatSink;
    //     //here we inject our event callback into each instance of BoatGoldController.
    //     boatGoldController.m_onGoldAddedToBoat = OnGoldAddedToBoat;
    //     //the first child of the boat spawn position is the gold drop zone spawn position.
    //     Transform goldDropZoneSpawn = m_boatSpawnLocationsPerTeam[teamNum - 1][boatNum - 1].GetChild(0).transform;
    //     boatGoldController.m_goldDropZone.transform.position = goldDropZoneSpawn.position;
    //     boatGoldController.m_goldDropZone.transform.localScale = goldDropZoneSpawn.localScale;
    //     boatGoldController.m_onBoatSail += OnBoatSail;
    // }
    
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

        GameObject boat = m_boatsPerTeam[teamNum - 1][boatNum - 1];
        
        //move the gold drop zone under the level while boat is sailing.
        boat.GetComponent<BoatGoldController>().m_goldDropZone.transform.position += new Vector3(0, -100, 0);
        
        boat.GetComponent<BoatData>().m_sinkAudioSource.Play();
        
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

        yield return new WaitForSeconds(m_boatResetTime);
        StopCoroutine(sinkAnimationCoroutine);
        
        ResetBoat(teamNum, boatNum, false);
        m_onResetBoat(teamNum, boatNum);
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
        
        GameObject boat = m_boatsPerTeam[teamNum - 1][boatNum - 1];
        //move the gold drop zone under the level while boat is sailing.
        boat.GetComponent<BoatGoldController>().m_goldDropZone.transform.position += new Vector3(0, -100, 0);
        
        boat.GetComponent<BoatData>().m_sailAudioSource.Play();
        
        Coroutine sailAnimationCoroutine = StartCoroutine(SailBoatAnimation(teamNum, boatNum));
        ScoreSystem.Instance.UpdateScore(goldScoredPerTeam);

        yield return new WaitForSeconds(m_boatResetTime);
        StopCoroutine(sailAnimationCoroutine);
        
        //respawn players that were on boat.
        foreach (Transform boardedPosition in m_boatsPerTeam[teamNum - 1][boatNum - 1].GetComponent<BoatData>()
                     .m_playerBoardedPositions)
        {
            GameObject player = boardedPosition.GetChild(0).gameObject;
            player.transform.parent = PlayerSystem.Instance.m_playersParents[player.GetComponent<PlayerData>().m_playerNum - 1];
            PlayerSystem.Instance.OnPlayerDie(player.GetComponent<PlayerData>().m_playerNum);
        }
        
        ResetBoat(teamNum, boatNum, true);
        m_onResetBoat(teamNum, boatNum);
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
