using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MyUILibrary;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.UI;
using UnityEngine.Serialization;
using UnityEngine.UIElements;


public class InGameHoversUI : UIBase
{
    private VisualElement m_root;

    [SerializeField] private VisualTreeAsset m_boatUIAsset;

    [SerializeField] private VisualTreeAsset m_playerUIAsset;

    [SerializeField] private List<GameObject> m_playerHoverPrefabs;

    [SerializeField] private List<Sprite> m_playerNumberIcons;
    
    [FormerlySerializedAs("m_goldInteractButtonIcon")] [SerializeField] private Sprite m_bombInteractButtonIcon;
    
    [SerializeField] private GameObject m_bombSpawnPointHoverPrefab;
    
    [SerializeField] private Sprite m_bombSpawnPointHoverIcon;
    
    [SerializeField] private List<GameObject> m_playerDashCooldownHoverPrefabs;
    
    [SerializeField] private List<Sprite> m_dashCooldownHoverIcons;

    [SerializeField] private GameObject m_goldScoringParticlesPrefab;

    [SerializeField] private float m_hoverIconScaleFactor = 0.01f;

    [SerializeField] private List<GameObject> m_boatHoverPrefabsPerTeam;

    [SerializeField] private Sprite m_boatHoverIcon;

    private List<List<VisualElement>> m_boatElements;

    private List<List<GameObject>> m_boatHoversPerTeam;

    private List<VisualElement> m_playerElements;

    private List<List<string>> m_currentBoatLabels;

    private List<GameObject> m_playerIndicators;

    private List<List<Coroutine>> m_boatLabelCoroutines;
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    protected override void SetUpUI()
    {
        m_boatElements = new List<List<VisualElement>>();
        
        m_playerElements = new List<VisualElement>();
        
        m_currentBoatLabels = new List<List<string>>();

        m_boatHoversPerTeam = new List<List<GameObject>>();
        
        m_playerIndicators = new List<GameObject>();

        m_boatLabelCoroutines = new List<List<Coroutine>>();
        
        m_root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("root");
        
        BoatSystem.Instance.m_onBoatDamaged += BombAddedToBoat;
        
        for (int i = 0; i < PlayerSystem.Instance.m_numPlayers; i++)
        {
            m_playerElements.Add(null);
            
            m_playerIndicators.Add(Instantiate(m_playerHoverPrefabs[i], new Vector3(0, 0, 0),
                Quaternion.identity));

            int teamAssignment = PlayerSystem.Instance.m_playerTeamAssignments[i];
        
            m_playerIndicators[i].GetComponent<GenericIndicatorController>().StartIndicator(0.05f,
                PlayerSystem.Instance.m_teamColors[teamAssignment - 1],
                hoverIcon: m_playerNumberIcons[i],
                objectToTrack: PlayerSystem.Instance.m_players[i],
                scaleFactor: m_hoverIconScaleFactor,
                camera: Camera.main,
                timeToLive: 10f);
        }

        for (int teamNum = 1; teamNum <= PlayerSystem.Instance.m_numTeams; teamNum++)
        {
            m_boatElements.Add(new List<VisualElement>());
            m_currentBoatLabels.Add(new List<string>());
            m_boatLabelCoroutines.Add(new List<Coroutine>());
            m_boatHoversPerTeam.Add(new List<GameObject>());
            
            for (int boatNum = 1; boatNum <= BoatSystem.Instance.m_numBoatsPerTeam[teamNum-1]; boatNum++)
            {
                m_boatElements[teamNum-1].Add(null);
                m_currentBoatLabels[teamNum-1].Add("");
                m_boatLabelCoroutines[teamNum - 1].Add(StartCoroutine(UpdateBoatUI(teamNum, boatNum)));
                m_boatHoversPerTeam[teamNum-1].Add(null);
            }
        }
        
        //set callback to update tutorial UI when player picks up gold
        PlayerSystem.Instance.m_onPlayerPickupBomb += OnGoldPickedUp;
        PlayerSystem.Instance.m_onPlayerDropBomb += OnGoldDropped;
        
        PlayerSystem.Instance.m_onDashCooldownStart += OnPlayerDashCooldownStart;
        
        BoatSystem.Instance.m_onSinkBoat += OnSinkBoat;

        GameTimerSystem.Instance.m_onGameStart += OnGameStart;
    }

    void OnDestroy()
    {
        PlayerSystem.Instance.m_onPlayerPickupBomb -= OnGoldPickedUp;
        PlayerSystem.Instance.m_onPlayerDropBomb -= OnGoldDropped;
        PlayerSystem.Instance.m_onDashCooldownStart -= OnPlayerDashCooldownStart;

        if (BoatSystem.Instance != null)
        {
            BoatSystem.Instance.m_onSinkBoat -= OnSinkBoat;
        }

        GameTimerSystem.Instance.m_onGameStart -= OnGameStart;

        foreach (List<Coroutine> boatCoroutines in m_boatLabelCoroutines)
        {
            foreach (Coroutine coroutine in boatCoroutines)
            {
                StopCoroutine(coroutine);
            }    
        }
    }

    void OnGameStart()
    {
        foreach (GameObject bombSpawnPoint in GameObject.FindGameObjectsWithTag("BombSpawnPoint"))
        {
            GameObject genericIndicatorInstance = Instantiate(m_bombSpawnPointHoverPrefab, Vector3.zero,
                Quaternion.identity);

        
            genericIndicatorInstance.GetComponent<GenericIndicatorController>().StartIndicator(0.08f,
                Color.yellow,
                hoverIcon: m_bombSpawnPointHoverIcon,
                objectToTrack: bombSpawnPoint,
                scaleFactor: m_hoverIconScaleFactor,
                camera: Camera.main
            );
            
            ClosingCircleSpawner.Instance.CreateClosingCircle(bombSpawnPoint, Color.white);
        }
    }

    void OnGoldPickedUp(int teamNum, int playerNum)
    {
        if (m_playerIndicators[playerNum - 1] != null)
        {
            Destroy(m_playerIndicators[playerNum - 1]);
        }
        //create popups if there isn't one already
        if (m_boatHoversPerTeam[teamNum - 1].Count == 0)
        {
            foreach (GameObject boat in GameObject.FindGameObjectsWithTag("Boat"))
            {
                if (boat.GetComponent<BoatData>().m_teamNum == teamNum)
                {
                    GameObject newHover = Instantiate(m_boatHoverPrefabsPerTeam[teamNum-1], Vector3.zero, Quaternion.identity);
                    
                    newHover.GetComponent<GenericIndicatorController>().StartIndicator(0.05f,
                        color: Color.white,
                        hoverIcon: m_boatHoverIcon,
                        objectToTrack: boat,
                        scaleFactor: m_hoverIconScaleFactor,
                        camera: Camera.main
                    );
                    
                    ClosingCircleSpawner.Instance.CreateClosingCircle(boat, Color.white);
                    
                    m_boatHoversPerTeam[teamNum - 1].Add(newHover);
                }
            }
        }
    }

    void OnGoldDropped(int teamNum, int playerNum)
    {
        if (m_playerIndicators[playerNum - 1] != null)
        {
            Destroy(m_playerIndicators[playerNum - 1]);
        }
        
        if (m_boatHoversPerTeam[teamNum - 1].Count != 0)
        {
            bool destroyHovers = true;
            foreach (GameObject player in PlayerSystem.Instance.m_players)
            {
                PlayerData playerData = player.GetComponent<PlayerData>();
                if (playerData.m_teamNum == teamNum &&
                    playerData.m_bombsCarried != 0)
                {
                    //dont destroy hovers
                    destroyHovers = false;
                    break;
                }
            }

            if (destroyHovers)
            {
                foreach (GameObject hover in m_boatHoversPerTeam[teamNum - 1])
                {
                    Destroy(hover);
                }

                m_boatHoversPerTeam[teamNum - 1].Clear();
            }
        }
    }
    

    private void OnPlayerEnterBombSpawnPoint(int teamNum, int playerNum)
    {
        if (PlayerSystem.Instance.m_players[playerNum - 1].GetComponent<PlayerData>().m_bombsCarried == 0)
        {
            if (m_playerIndicators[playerNum - 1] != null)
            {
                Destroy(m_playerIndicators[playerNum - 1]);
            }

            m_playerIndicators[playerNum - 1] = Instantiate(m_playerHoverPrefabs[playerNum - 1],
                new Vector3(0, 0, 0), Quaternion.identity);

            m_playerIndicators[playerNum - 1].GetComponent<GenericIndicatorController>().StartIndicator(0.05f,
                PlayerSystem.Instance.m_teamColors[teamNum - 1],
                hoverIcon: m_bombInteractButtonIcon,
                objectToTrack: PlayerSystem.Instance.m_players[playerNum - 1],
                scaleFactor: m_hoverIconScaleFactor,
                camera: Camera.main
            );
        }
    }
    
    private void OnPlayerLeaveBombSpawnPoint(int teamNum, int playerNum)
    {
        if (m_playerIndicators[playerNum - 1] != null)
        {
            Destroy(m_playerIndicators[playerNum - 1]);
        }
    }
    
    private void OnPlayerEnterEnemyBoatRadius(int teamNum, int playerNum)
    {
        if (PlayerSystem.Instance.m_players[playerNum - 1].GetComponent<PlayerData>().m_bombsCarried > 0)
        {
            if (m_playerIndicators[playerNum - 1] != null)
            {
                Destroy(m_playerIndicators[playerNum - 1]);
            }

            m_playerIndicators[playerNum - 1] = Instantiate(m_playerHoverPrefabs[playerNum - 1],
                new Vector3(0, 0, 0), Quaternion.identity);

            m_playerIndicators[playerNum - 1].GetComponent<GenericIndicatorController>().StartIndicator(0.05f,
                PlayerSystem.Instance.m_teamColors[teamNum - 1],
                hoverIcon: m_bombInteractButtonIcon,
                objectToTrack: PlayerSystem.Instance.m_players[playerNum - 1],
                scaleFactor: m_hoverIconScaleFactor,
                camera: Camera.main
            );
        }
    }
    
    private void OnPlayerExitEnemyBoatRadius(int teamNum, int playerNum)
    {
        if (m_playerIndicators[playerNum - 1] != null)
        {
            Destroy(m_playerIndicators[playerNum - 1]);
        }
    }

    private void OnPlayerDashCooldownStart(int teamNum, int playerNum, float cooldownSeconds)
    {

        StartCoroutine(DashCooldownHoverAnimation(teamNum, playerNum, cooldownSeconds));
    }

    private IEnumerator DashCooldownHoverAnimation(int teamNum, int playerNum, float cooldownSeconds)
    {
        if (m_playerIndicators[playerNum - 1])
        {
            // GameObject newHover = Instantiate(m_playerDashCooldownHoverPrefabs[playerNum - 1], Vector3.zero,
            //     Quaternion.identity);
            //
            // newHover.GetComponent<GenericIndicatorController>().StartIndicator(0.05f,
            //     color: Color.white,
            //     hoverIcon: m_dashCooldownHoverIcons[playerNum - 1],
            //     objectToTrack: PlayerSystem.Instance.m_players[playerNum - 1],
            //     scaleFactor: m_hoverIconScaleFactor,
            //     camera: Camera.main
            // );
            //
            // m_playerIndicators[playerNum - 1] = newHover;
            //
            // yield return new WaitForSeconds(cooldownSeconds);
            //
            // if (m_playerIndicators[playerNum - 1] == newHover)
            // {
            //     Destroy(m_playerIndicators[playerNum - 1]);
            //     m_playerIndicators[playerNum - 1] = null;
            // }
        }
        else
        {
            yield return null;
        }
    } 
    
    private void OnSinkBoat(int teamNum, int boatNum)
    {
        BoatDeleted(teamNum, boatNum);
    }

    
    private void BoatDeleted(int teamNum, int boatNum)
    {
        m_boatElements[teamNum-1][boatNum-1].Clear();
        m_boatElements[teamNum-1][boatNum-1].RemoveFromHierarchy();
        m_boatElements[teamNum-1][boatNum-1] = null;
    }

    void BombAddedToBoat(int teamNum, int boatNum, int damage, int remainingHealth, int maxHealth)
    {
        Debug.Log($"adding bomb to boat team: {teamNum}, boat: {boatNum}");
        m_currentBoatLabels[teamNum-1][boatNum-1] = $"{remainingHealth} / {maxHealth}";
        
        GameObject boatObject = BoatSystem.Instance.m_boatsPerTeam[teamNum-1][boatNum-1];

        GameObject boatModelObject = null;
        for (int i = 0; i < boatObject.transform.childCount; i++)
        {
            Transform child = boatObject.transform.GetChild(i);
            if (child.CompareTag("BoatModel"))
            {
                boatModelObject = child.gameObject;
                break;
            }
        }
    }

    IEnumerator UpdateBoatUI(int teamNum, int boatNum)
    {
        GameObject boat = BoatSystem.Instance.m_boatsPerTeam[teamNum-1][boatNum-1];
        BoatData boatData = boat.GetComponent<BoatData>();
        m_currentBoatLabels[teamNum-1][boatNum-1] = $"{boatData.m_remainingHealth}/{boatData.m_maxHealth}";

        GameObject boatHoverTrackLocation = null;

        for (int childNum = 0; childNum < boat.transform.childCount; childNum++)
        {
            Transform child = boat.transform.GetChild(childNum);
            if (child.CompareTag("BoatHoverTrackLocation"))
            {
                boatHoverTrackLocation = child.gameObject;
            }
        }
        
        m_boatElements[teamNum-1][boatNum-1] = m_boatUIAsset.Instantiate();
        m_root.Add(m_boatElements[teamNum-1][boatNum-1]);
        
        Label capacityLabel = m_boatElements[teamNum-1][boatNum-1].Q<Label>("capacity-label");

        while (true)
        {
            Vector3 screen = Camera.main.WorldToScreenPoint(boatHoverTrackLocation.transform.position);
            float screenX = Screen.width * screen.x / Camera.main.pixelWidth;
            float screenY = Screen.height * screen.y / Camera.main.pixelHeight;
            m_boatElements[teamNum - 1][boatNum - 1].style.left = screenX;
            m_boatElements[teamNum-1][boatNum-1].style.top = (Screen.height - screenY) - 50;

            // timerElement.progress = boatTimerController.m_currentTimeToLive;

            capacityLabel.text = m_currentBoatLabels[teamNum-1][boatNum-1];
        
            yield return null;
        }
    }
    
}
