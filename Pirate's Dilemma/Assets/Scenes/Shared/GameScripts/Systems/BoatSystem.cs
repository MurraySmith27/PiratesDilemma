using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public delegate void DeleteBoatEvent(int teamNum, int boatNum);
public delegate void SpawnBoatEvent(int teamNum, int boatNum);
public delegate void GoldAddedToBoatEvent(int teamNum, int boatNum, int currentGold, int capacity);
public class BoatSystem : MonoBehaviour
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
    
    [SerializeField] private const float m_boatRespawnTime = 5f;

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

    private List<List<GameObject>> m_boatsPerTeam;

    private List<List<Transform>> m_boatSpawnLocationsPerTeam;
    
    void Start()
    {
        SetBoatSpawnPositions();
        SceneManager.sceneLoaded += OnGameSceneLoaded;
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
        //Spawn boats
        if (boatNum == m_boatsPerTeam[teamNum].Count)
        {
            m_boatsPerTeam[teamNum].Add(null);
        }
        
        GameObject newBoat = Instantiate(m_boatPrefabsPerTeam[teamNum], 
            m_boatSpawnLocationsPerTeam[teamNum][boatNum].transform.position, Quaternion.identity);
        
        m_boatsPerTeam[teamNum][boatNum] = newBoat;

        BoatData boatData = newBoat.GetComponent<BoatData>();
        boatData.m_teamNum = teamNum;
        boatData.m_boatNum = boatNum;
        boatData.m_timeToLive = new System.Random().Next(m_boatMinTimeToLive, m_boatMaxTimeToLive);
        boatData.m_goldCapacity = new System.Random().Next(m_boatMinCapacity, m_boatMaxCapacity);

        BoatGoldController boatGoldController = newBoat.GetComponent<BoatGoldController>();
        boatGoldController.m_onBoatSink += OnBoatSink;
        //here we inject our event callback into each instance of BoatGoldController.
        boatGoldController.m_onGoldAddedToBoat = this.m_onGoldAddedToBoat;

        BoatTimerController boatTimerController = newBoat.GetComponent<BoatTimerController>();
        boatTimerController.m_onBoatSail += OnBoatSail;
    }
    
    private void OnBoatSink(int teamNum, int boatNum)
    {
        StartCoroutine(SinkBoat(teamNum, boatNum));
    }
    
    private IEnumerator SinkBoat(int teamNum, int boatNum)
    {
        m_onDeleteBoat(teamNum, boatNum);
        Coroutine sinkAnimationCoroutine = StartCoroutine(SinkBoatAnimation());

        yield return new WaitForSeconds(m_boatRespawnTime);
        StopCoroutine(sinkAnimationCoroutine);
        
        Destroy(m_boatsPerTeam[teamNum][boatNum]);
        
        SpawnBoat(teamNum, boatNum);
        m_onSpawnBoat(teamNum, boatNum);
    }
    
    IEnumerator SinkBoatAnimation()
    {
        m_boatSinkSound.Play();
        
        while (true)
        {
            transform.Translate(new Vector3(0, -50, 0) * Time.deltaTime);
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
        Coroutine sailAnimationCoroutine = StartCoroutine(SailBoatAnimation());

        yield return new WaitForSeconds(m_boatRespawnTime);
        StopCoroutine(sailAnimationCoroutine);
        
        
        ScoreSystem.Instance.UpdateScore(goldScoredPerTeam);
        
        Destroy(m_boatsPerTeam[teamNum][boatNum]);
        
        SpawnBoat(teamNum, boatNum);
        m_onSpawnBoat(teamNum, boatNum);
    }

    IEnumerator SailBoatAnimation()
    {
        m_boatSailSound.Play();
        
        while (true)
        {
            transform.Translate(new Vector3(-10, 0, 0) * Time.deltaTime);
            yield return null;
        }
    }
    
    private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //only call the callback for the next scene loaded.
        SceneManager.sceneLoaded -= OnGameSceneLoaded;
        
        if (GameStartSystem.Instance.m_levelSceneNames.Contains(scene.name))
        {
            //get spawn positions from gameobjects in scene with special tags.
            SetBoatSpawnPositions();

            for (int teamNum = 0; teamNum < PlayerSystem.Instance.m_numTeams; teamNum++)
            {
                m_numBoatsPerTeam[teamNum] = m_boatSpawnLocationsPerTeam[teamNum].Count;
                
                //Spawn boats
                for (int boatNum = 0; boatNum < m_numBoatsPerTeam[teamNum]; boatNum++)
                {
                    SpawnBoat(teamNum, boatNum);
                }
            }
        }
    }
    
}
