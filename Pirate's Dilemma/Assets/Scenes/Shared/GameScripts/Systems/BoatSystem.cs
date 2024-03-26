using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public delegate void ResetBoatEvent(int teamNum, int boatNum);
public delegate void BoatDamagedEvent(int teamNum, int boatNum, int damage, int remainingHealth, int maxHealth);
public class BoatSystem : GameSystem
{
    private static BoatSystem _instance;
    
    public static BoatSystem Instance
    {
        get { return _instance; }
    }

    public BoatSinkEvent m_onSinkBoat;
    public ResetBoatEvent m_onResetBoat;
    public BoatDamagedEvent m_onBoatDamaged;

    [SerializeField] private float m_sinkDistance = 1f;

    [SerializeField] private int m_boatMaxHealth = 5;
    
    public int m_numBoats
    {
        get { return m_numBoatsPerTeam.Sum(); }
    }

    [HideInInspector] public List<int> m_boatTeamAssignments;

    [HideInInspector] public List<int> m_numBoatsPerTeam;

    [HideInInspector] public List<int> m_boatNumToTeamBoatNum {
        get
        {
            List<int> arr = new();

            List<int> numBoatsSoFarPerTeam = new();
            for (int i = 0; i < PlayerSystem.Instance.m_numTeams; i++)
            {
                numBoatsSoFarPerTeam.Add(0);
            }
            for (int i = 0; i < m_boatTeamAssignments.Count; i++)
            {
                arr.Add(++numBoatsSoFarPerTeam[m_boatTeamAssignments[i]-1]);
            }

            return arr;
        }
        private set {}
    }

    [HideInInspector]
    public List<List<int>> m_teamBoatNumToBoatNum
    {
        get
        {
            List<List<int>> arr = new();

            for (int i = 0; i < PlayerSystem.Instance.m_numTeams; i++)
            {
                List<int> teamToBoatNumForThisTeam = new();
                int teamNum = i + 1;
                for (int j = 0; j < m_boatTeamAssignments.Count; j++)
                {
                    if (m_boatTeamAssignments[j] == teamNum)
                    {
                        teamToBoatNumForThisTeam.Add(j+1);
                    }
                }
                arr.Add(teamToBoatNumForThisTeam);
            }

            return arr;
        }
        private set {}
    }

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
    

    protected override void OnDestroy()
    {
        if (_instance == this)
        {
            base.OnDestroy();
            _instance = null;
        }
        GameTimerSystem.Instance.m_onGameStart -= OnGameStart;
        GameTimerSystem.Instance.m_onGameFinish -= OnGameFinish;
    }
    
    void Start()
    {
        
        GameTimerSystem.Instance.m_onGameStart += OnGameStart;
        GameTimerSystem.Instance.m_onGameFinish += OnGameFinish;
        
        
        m_boatInitialPositionsPerTeam = new List<List<Vector3>>();
        m_boatsPerTeam = new List<List<GameObject>>();
        m_boatTeamAssignments = new List<int>();
        
        for (int i = 0; i < PlayerSystem.Instance.m_numTeams; i++)
        {
            GameObject[] teamBoats = GameObject.FindGameObjectsWithTag($"Team{i + 1}Boat");
            m_boatsPerTeam.Add(new List<GameObject>());
            
            m_numBoatsPerTeam.Add(teamBoats.Length);
            
            m_boatInitialPositionsPerTeam.Add(new List<Vector3>());
            
            for (int j = 0; j < teamBoats.Length; j++)
            {
                GameObject boat = teamBoats[j].transform.GetChild(0).gameObject;
                BoatData boatData = boat.GetComponent<BoatData>();
                boatData.m_teamNum = i + 1;
                boatData.m_boatNum = j + 1;
                boatData.m_maxHealth = m_boatMaxHealth;
                boatData.m_remainingHealth = boatData.m_maxHealth;
                
                //inject our callbacks
                BoatDamageController boatDamageController = boat.GetComponent<BoatDamageController>();
                boatDamageController.m_onBoatSink += OnBoatSink;
                boatDamageController.m_onBoatDamaged = OnBoatDamaged;

                BoatAnimationController boatAnimationController = boat.GetComponent<BoatAnimationController>();

                boatAnimationController.SetBoatTeamMaterials(PlayerSystem.Instance.m_teamData[i].mainMaterial,
                    PlayerSystem.Instance.m_teamData[i].accentMaterial);
                
                m_boatTeamAssignments.Add(i+1);
                
                m_boatsPerTeam[i].Add(boat);
                m_boatInitialPositionsPerTeam[i].Add(boat.transform.position);
            }
        }
        
        base.SystemReady();
    }

    private void OnGameStart()
    {
        foreach (GameObject boat in m_boats)
        {
            boat.GetComponent<BoatDamageController>().m_acceptingDamage = true;
        }
    }
    
    private void OnGameFinish()
    {
        foreach (GameObject boat in m_boats)
        {
            boat.GetComponent<BoatDamageController>().m_acceptingDamage = false;
        }
    }
    
    
    private void OnBoatDamaged(int teamNum, int boatNum, int damage, int remainingHealth, int maxHealth)
    {
        m_onBoatDamaged(teamNum, boatNum, damage, remainingHealth, maxHealth);
    }
    
    private void OnBoatSink(int teamNum, int boatNum)
    {
        StartCoroutine(SinkBoat(teamNum, boatNum));
    }
    
    private IEnumerator SinkBoat(int teamNum, int boatNum)
    {
        //do the animation:
        GameObject boat = m_boatsPerTeam[teamNum - 1][boatNum - 1];
        Vector3 initialPos = m_boatsPerTeam[teamNum - 1][boatNum - 1].transform.position;

        Vector3 finalPos = initialPos + new Vector3(0, -m_sinkDistance, 0);

        boat.GetComponent<BoatData>().m_sinkEventEmitter.Play();
        for (float t = 0; t < 1; t += Time.deltaTime)
        {
            m_boatsPerTeam[teamNum - 1][boatNum - 1].transform.position = Vector3.Lerp(initialPos, finalPos, t);
            yield return null;
        }
        
        m_onSinkBoat(teamNum, boatNum);

        
        

    }
    
}
