using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BoatSystem : MonoBehaviour
{

    private static BoatSystem _instance;
    
    public static BoatSystem Instance
    {
        get { return _instance; }
    }

    [SerializeField] private int boatMinTimeToLive;
    [SerializeField] private int boatMaxTimeToLive;

    [SerializeField] private int boatMinCapacity;
    [SerializeField] private int boatMaxCapacity;

    [SerializeField] private GameObject team1BoatPrefab;
    [SerializeField] private GameObject team2BoatPrefab;
    
    public int m_numBoats {get {return m_numTeam1Boats + m_numTeam2Boats}}
    
    [HideInInspector] public int m_numTeam1Boats;
    [HideInInspector] public int m_numTeam2Boats;
    
    private List<GameObject> m_boats {get {return m_team1Boats.Concat(m_team2Boats)}};
    private List<GameObject> m_team1Boats;
    private List<GameObject> m_team2Boats;
    private List<Transform> m_team1BoatSpawnLocations;
    private List<Transform> m_team2BoatSpawnLocations;
    
    
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
        
        m_boats = new List<GameObject>();
    }
    
    private void SetBoatSpawnPositions()
    {
        m_team1BoatSpawnLocations = new List<Transform>();
        m_team2BoatSpawnLocations = new List<Transform>();
        
        foreach (GameObject gameObject in GameObject.FindGameObjectsWithTag("Team1BoatSpawn"))
        {
            m_team1BoatSpawnLocations.Add(gameObject.transform);
        }
        
        foreach (GameObject gameObject in GameObject.FindGameObjectsWithTag("Team2BoatSpawn"))
        {
            m_team2BoatSpawnLocations.Add(gameObject.transform);
        }
    }
    

    public int RespawnBoat(int boatSlot)
    {
            m_boats[boatSlot] = GameObject.Instantiate(Resources.Load("Shiptest_prefab"), 
                m_boatSpawnLocations[boatSlot].transform.position, Quaternion.identity) as GameObject;
            BoatController boatController = m_boats[boatSlot].GetComponent<BoatController>();
            boatController.timeToLive = new System.Random().Next(20, 30);
            boatController.boatTotalCapacity = Random.Range(60, 80);
            boatController.boatSlot = boatSlot;
            boatController.boatSystem = this;
            return m_boats[boatSlot].GetInstanceID();
    }
    
    private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //only call the callback for the next scene loaded.
        SceneManager.sceneLoaded -= OnGameSceneLoaded;
        
        if (GameStartSystem.Instance.m_levelSceneNames.Contains(scene.name))
        {
            //get spawn positions from gameobjects in scene with special tags.
            SetBoatSpawnPositions();

            m_numTeam1Boats = m_team1BoatSpawnLocations.Count;
            m_numTeam2Boats = m_team2BoatSpawnLocations.Count;
            
            //Spawn boats
            for (int i = 0; i < m_numTeam1Boats; i++) {
                m_boats.Add(Instantiate(team1BoatPrefab, 
                    m_team1BoatSpawnLocations[i].transform.position, Quaternion.identity));
                BoatController boatController = m_boats[i].GetComponent<BoatController>();
                boatController.timeToLive = new System.Random().Next(20, 30);
                boatController.boatTotalCapacity = Random.Range(60, 80);
                boatController.boatSlot = i;
                boatController.boatSystem = this;
            
            }
        }
    }
    
}
