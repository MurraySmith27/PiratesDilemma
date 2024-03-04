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
using UnityEngine.UIElements;


public class InGameHoversUI : UIBase
{
    private VisualElement m_root;

    [SerializeField] private VisualTreeAsset m_boatUIAsset;

    [SerializeField] private VisualTreeAsset m_playerUIAsset;

    [SerializeField] private List<GameObject> m_playerHoverPrefabs;

    [SerializeField] private List<Sprite> m_playerNumberIcons;

    [SerializeField] private GameObject m_goldPickupZoneHoverPrefab;

    [SerializeField] private Sprite m_goldPickupZoneHoverIcon;

    [SerializeField] private Sprite m_goldInteractButtonIcon;

    [SerializeField] private List<GameObject> m_goldDropZoneHoverPrefabsPerTeam;

    [SerializeField] private Sprite m_goldDropZoneHoverIcon;

    [SerializeField] private List<GameObject> m_playerDashCooldownHoverPrefabs;
    
    [SerializeField] private List<Sprite> m_dashCooldownHoverIcons;

    [SerializeField] private List<GameObject> m_boatBoardingHoverPrefabs;

    [SerializeField] private List<Sprite> m_team1BoatBoardingHoverIcons;

    [SerializeField] private List<Sprite> m_team2BoatBoardingHoverIcons;

    [SerializeField] private float m_hoverIconScaleFactor = 0.1f;

    private List<List<Sprite>> m_boardingHoverIconsPerTeam
    {
        get
        {
            List<List<Sprite>> arr = new();
            arr.Add(m_team1BoatBoardingHoverIcons);
            arr.Add(m_team2BoatBoardingHoverIcons);
            
            return arr;
        }
    }


    private List<List<VisualElement>> m_boatElements;

    private List<VisualElement> m_playerElements;

    private List<List<Coroutine>> m_boatTimerLabelCoroutines;

    private List<List<string>> m_currentBoatLabels;

    private List<List<GameObject>> m_dropZoneHoversPerTeam;

    private List<List<List<GameObject>>> m_boatBoardingHoversPerBoatPerTeam;

    private List<GameObject> m_playerIndicators;
    

    protected override void Awake()
    {
        base.Awake();
    }
    

    protected override void SetUpUI()
    {
        m_boatTimerLabelCoroutines = new List<List<Coroutine>>();

        m_boatElements = new List<List<VisualElement>>();
        
        m_playerElements = new List<VisualElement>();
        
        m_currentBoatLabels = new List<List<string>>();

        m_dropZoneHoversPerTeam = new List<List<GameObject>>();

        m_boatBoardingHoversPerBoatPerTeam = new List<List<List<GameObject>>>();
        
        m_playerIndicators = new List<GameObject>();
        
        m_root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("root");
        
        BoatSystem.Instance.m_onResetBoat += NewBoatSpawned;
        BoatSystem.Instance.m_onGoldAddedToBoat += GoldAddedToBoat;
        
        for (int i = 0; i < PlayerSystem.Instance.m_numPlayers; i++)
        {
            m_playerElements.Add(null);
            
            m_playerIndicators.Add(Instantiate(m_playerHoverPrefabs[i], new Vector3(0, 0, 0),
                Quaternion.identity));

            int teamAssignment = PlayerSystem.Instance.m_playerTeamAssignments[i];
        
            m_playerIndicators[i].GetComponent<GenericIndicatorController>().StartIndicator(0.1f,
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
            m_boatTimerLabelCoroutines.Add(new List<Coroutine>());
            m_currentBoatLabels.Add(new List<string>());
            m_dropZoneHoversPerTeam.Add(new List<GameObject>());
            m_boatBoardingHoversPerBoatPerTeam.Add(new List<List<GameObject>>());
            
            for (int boatNum = 1; boatNum <= BoatSystem.Instance.m_numBoatsPerTeam[teamNum-1]; boatNum++)
            {
                m_boatElements[teamNum-1].Add(null);
                m_currentBoatLabels[teamNum-1].Add("");
                m_boatTimerLabelCoroutines[teamNum-1].Add( StartCoroutine(UpdateBoatUI(teamNum, boatNum)));
                m_boatBoardingHoversPerBoatPerTeam[teamNum - 1].Add(new List<GameObject>());

                for (int playerNum = 1; playerNum <= PlayerSystem.Instance.m_numPlayersPerTeam[teamNum - 1]; playerNum++)
                {
                    m_boatBoardingHoversPerBoatPerTeam[teamNum - 1][boatNum - 1].Add(null);
                }
            }
        }
        
        
        //set callback to update tutorial UI when player picks up gold
        PlayerSystem.Instance.m_onPlayerPickupGold += OnGoldPickedUp;
        PlayerSystem.Instance.m_onPlayerDropGold += OnGoldDropped;
        PlayerSystem.Instance.m_onPlayerBoardBoat += OnPlayerBoardedBoat;
        PlayerSystem.Instance.m_onPlayerGetOffBoat += OnPlayerGetOffBoat;

        PlayerSystem.Instance.m_onPlayerEnterGoldPickupZone += OnPlayerEnterGoldPickupZone;
        PlayerSystem.Instance.m_onPlayerExitGoldPickupZone += OnPlayerExitGoldPickupZone;
        PlayerSystem.Instance.m_onPlayerEnterGoldDropZone += OnPlayerEnterGoldDropZone;
        PlayerSystem.Instance.m_onPlayerExitGoldDropZone += OnPlayerExitGoldDropZone;
        
        PlayerSystem.Instance.m_onDashCooldownStart += OnPlayerDashCooldownStart;
        
        BoatSystem.Instance.m_onSailBoat += OnSailBoat;
        BoatSystem.Instance.m_onSinkBoat += OnSinkBoat;

        GameTimerSystem.Instance.m_onGameStart += OnGameStart;

    }

    void OnGameStart()
    {
        foreach (GameObject goldPickupZone in GameObject.FindGameObjectsWithTag("GoldPickupZone"))
        {
            GameObject genericIndicatorInstance = Instantiate(m_goldPickupZoneHoverPrefab, Vector3.zero,
                Quaternion.identity);

        
            genericIndicatorInstance.GetComponent<GenericIndicatorController>().StartIndicator(0.2f,
                Color.black,
                hoverIcon: m_goldPickupZoneHoverIcon,
                objectToTrack: goldPickupZone,
                scaleFactor: m_hoverIconScaleFactor,
                camera: Camera.main
            );
            
            ClosingCircleSpawner.Instance.CreateClosingCircle(goldPickupZone, Color.white);
        }
    }

    void OnGoldPickedUp(int teamNum, int playerNum)
    {
        if (m_playerIndicators[playerNum - 1] != null)
        {
            Destroy(m_playerIndicators[playerNum - 1]);
        }
        //create popups if there isn't one already
        if (m_dropZoneHoversPerTeam[teamNum - 1].Count == 0)
        {
            foreach (GameObject goldDropZone in GameObject.FindGameObjectsWithTag("BoatDropZone"))
            {
                if (goldDropZone.GetComponentInChildren<GoldDropZoneData>().m_boat.GetComponent<BoatData>()
                        .m_teamNum == teamNum)
                {
                    GameObject newHover = Instantiate(m_goldDropZoneHoverPrefabsPerTeam[teamNum-1], Vector3.zero, Quaternion.identity);
                    
                    newHover.GetComponent<GenericIndicatorController>().StartIndicator(0.1f,
                        color: PlayerSystem.Instance.m_teamColors[teamNum - 1],
                        hoverIcon: m_goldDropZoneHoverIcon,
                        objectToTrack: goldDropZone,
                        scaleFactor: m_hoverIconScaleFactor,
                        camera: Camera.main
                    );
                    
                    ClosingCircleSpawner.Instance.CreateClosingCircle(goldDropZone, Color.white);
                    
                    m_dropZoneHoversPerTeam[teamNum - 1].Add(newHover);
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
        
        if (m_dropZoneHoversPerTeam[teamNum - 1].Count != 0)
        {
            bool destroyHovers = true;
            foreach (GameObject player in PlayerSystem.Instance.m_players)
            {
                PlayerData playerData = player.GetComponent<PlayerData>();
                if (playerData.m_teamNum == teamNum &&
                    playerData.m_goldCarried != 0)
                {
                    //dont destroy hovers
                    destroyHovers = false;
                    break;
                }
            }

            if (destroyHovers)
            {
                foreach (GameObject hover in m_dropZoneHoversPerTeam[teamNum - 1])
                {
                    Destroy(hover);
                }

                m_dropZoneHoversPerTeam[teamNum - 1].Clear();
            }
        }
    }

    private void OnPlayerBoardedBoat(int teamNum, int playerNum, int boatNum)
    {
        playerNum = PlayerSystem.Instance.m_playerNumToTeamPlayerNum[playerNum - 1]; 
        //remove one popup
        if (m_boatBoardingHoversPerBoatPerTeam[teamNum - 1][boatNum - 1][playerNum - 1] != null)
        {
            Destroy(m_boatBoardingHoversPerBoatPerTeam[teamNum - 1][boatNum - 1][playerNum - 1]);
        }
    }

    private void OnPlayerGetOffBoat(int teamNum, int playerNum, int boatNum)
    {
        int teamPlayerNum = PlayerSystem.Instance.m_playerNumToTeamPlayerNum[playerNum - 1];
        int globalBoatNum = BoatSystem.Instance.m_teamBoatNumToBoatNum[teamNum-1][boatNum - 1];
        
        GameObject boatObject = BoatSystem.Instance.m_boats[globalBoatNum - 1];
        int numGoldOnBoat = boatObject.GetComponent<BoatData>().m_currentTotalGoldStored;

        int numPlayersOnThisTeam = PlayerSystem.Instance.m_numPlayersPerTeam[teamNum - 1];

        
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
        
        if (m_boatBoardingHoversPerBoatPerTeam[teamNum - 1][boatNum - 1][teamPlayerNum - 1] == null && numGoldOnBoat > 0)
        {
            //create a new popup.
            GameObject newHover = Instantiate(m_boatBoardingHoverPrefabs[playerNum-1], Vector3.zero, Quaternion.identity);

            newHover.GetComponent<GenericIndicatorController>().StartIndicator(0.1f,
                color: Color.white,
                hoverIcon: m_boardingHoverIconsPerTeam[teamNum - 1][teamPlayerNum - 1],
                horizontalOffset: 0.3f * (0.5f + teamPlayerNum - numPlayersOnThisTeam / 2f) * m_hoverIconScaleFactor,
                objectToTrack: boatModelObject,
                scaleFactor: m_hoverIconScaleFactor,
                camera: Camera.main
            );
            
            
            ClosingCircleSpawner.Instance.CreateClosingCircle(boatModelObject, Color.white);

            m_boatBoardingHoversPerBoatPerTeam[teamNum-1][boatNum-1][teamPlayerNum-1] = newHover;
        }
    }

    private void OnPlayerEnterGoldPickupZone(int teamNum, int playerNum)
    {
        if (PlayerSystem.Instance.m_players[playerNum - 1].GetComponent<PlayerData>().m_goldCarried == 0)
        {
            if (m_playerIndicators[playerNum - 1] != null)
            {
                Destroy(m_playerIndicators[playerNum - 1]);
            }

            m_playerIndicators[playerNum - 1] = Instantiate(m_playerHoverPrefabs[playerNum - 1],
                new Vector3(0, 0, 0), Quaternion.identity);

            m_playerIndicators[playerNum - 1].GetComponent<GenericIndicatorController>().StartIndicator(0.1f,
                PlayerSystem.Instance.m_teamColors[teamNum - 1],
                hoverIcon: m_goldInteractButtonIcon,
                objectToTrack: PlayerSystem.Instance.m_players[playerNum - 1],
                scaleFactor: m_hoverIconScaleFactor,
                camera: Camera.main
            );
        }
    }
    
    private void OnPlayerExitGoldPickupZone(int teamNum, int playerNum)
    {
        if (m_playerIndicators[playerNum - 1] != null)
        {
            Destroy(m_playerIndicators[playerNum - 1]);
        }
    }
    
    private void OnPlayerEnterGoldDropZone(int teamNum, int playerNum)
    {
        if (PlayerSystem.Instance.m_players[playerNum - 1].GetComponent<PlayerData>().m_goldCarried > 0)
        {
            if (m_playerIndicators[playerNum - 1] != null)
            {
                Destroy(m_playerIndicators[playerNum - 1]);
            }

            m_playerIndicators[playerNum - 1] = Instantiate(m_playerHoverPrefabs[playerNum - 1],
                new Vector3(0, 0, 0), Quaternion.identity);

            m_playerIndicators[playerNum - 1].GetComponent<GenericIndicatorController>().StartIndicator(0.1f,
                PlayerSystem.Instance.m_teamColors[teamNum - 1],
                hoverIcon: m_goldInteractButtonIcon,
                objectToTrack: PlayerSystem.Instance.m_players[playerNum - 1],
                scaleFactor: m_hoverIconScaleFactor,
                camera: Camera.main
            );
        }
    }
    
    private void OnPlayerExitGoldDropZone(int teamNum, int playerNum)
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
            GameObject newHover = Instantiate(m_playerDashCooldownHoverPrefabs[playerNum - 1], Vector3.zero,
                Quaternion.identity);

            newHover.GetComponent<GenericIndicatorController>().StartIndicator(0.1f,
                color: Color.white,
                hoverIcon: m_dashCooldownHoverIcons[playerNum - 1],
                objectToTrack: PlayerSystem.Instance.m_players[playerNum - 1],
                scaleFactor: m_hoverIconScaleFactor,
                camera: Camera.main
            );
            
            m_playerIndicators[playerNum - 1] = newHover;

            yield return new WaitForSeconds(cooldownSeconds);

            if (m_playerIndicators[playerNum - 1] == newHover)
            {
                Destroy(m_playerIndicators[playerNum - 1]);
                m_playerIndicators[playerNum - 1] = null;
            }
        }
        else
        {
            yield return null;
        }
    } 

    private void OnSailBoat(int teamNum, int boatNum, List<int> goldScoredPerTeam)
    {
        BoatDeleted(teamNum, boatNum);
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
        StopCoroutine(m_boatTimerLabelCoroutines[teamNum-1][boatNum-1]);
        
        //get rid of boat boarding alert popups
        for (int i = 0; i < m_boatBoardingHoversPerBoatPerTeam[teamNum-1][boatNum-1].Count; i++) 
        {
            Destroy(m_boatBoardingHoversPerBoatPerTeam[teamNum-1][boatNum-1][i]);
            m_boatBoardingHoversPerBoatPerTeam[teamNum-1][boatNum-1][i] = null;
        }
        
    }

    void NewBoatSpawned(int teamNum, int boatNum)
    {
        m_boatTimerLabelCoroutines[teamNum-1][boatNum-1] = StartCoroutine(UpdateBoatUI(teamNum, boatNum));
    }

    void GoldAddedToBoat(int teamNum, int boatNum, int goldTotal, int capacity)
    {
        m_currentBoatLabels[teamNum-1][boatNum-1] = $"{goldTotal} / {capacity}";
        
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
        
        int numPlayersOnThisTeam = PlayerSystem.Instance.m_numPlayersPerTeam[teamNum - 1];
        
        //add a hover to tell players to get on the boat (if there isn't one already) for first player per team (boat side)
        int globalPlayerNum = PlayerSystem.Instance.m_teamPlayerNumToPlayerNum[teamNum-1][0];
        if (m_boatBoardingHoversPerBoatPerTeam[teamNum - 1][boatNum - 1][0] == null)
        {
            GameObject newHover = Instantiate(m_boatBoardingHoverPrefabs[globalPlayerNum-1], Vector3.zero, Quaternion.identity);

            newHover.GetComponent<GenericIndicatorController>().StartIndicator(0.1f,
                color: Color.white,
                hoverIcon: m_boardingHoverIconsPerTeam[teamNum - 1][0],
                objectToTrack: boatModelObject,
                scaleFactor: m_hoverIconScaleFactor,
                camera: Camera.main
            );

            m_boatBoardingHoversPerBoatPerTeam[teamNum-1][boatNum-1][0] = newHover;
        }
    }

    IEnumerator UpdateBoatUI(int teamNum, int boatNum)
    {
        GameObject boat = BoatSystem.Instance.m_boatsPerTeam[teamNum-1][boatNum-1];
        BoatData boatData = boat.GetComponent<BoatData>();
        BoatTimerController boatTimerController = boat.GetComponent<BoatTimerController>();
        int initialTimeToLive = boatData.GetComponent<BoatData>().m_timeToLive;
        m_currentBoatLabels[teamNum-1][boatNum-1] = $"{boatData.m_currentTotalGoldStored}/{boatData.m_goldCapacity}";

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

        // RadialProgress timerElement = m_boatElements[teamNum-1][boatNum-1].Q<RadialProgress>("radial-timer");
        // timerElement.maxTotalProgress = initialTimeToLive;

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
