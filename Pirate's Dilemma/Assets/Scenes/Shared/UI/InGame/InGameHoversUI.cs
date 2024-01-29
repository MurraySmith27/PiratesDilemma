using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MyUILibrary;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class InGameHoversUI : UIBase
{
    private VisualElement m_root;

    [SerializeField] private VisualTreeAsset m_boatUIAsset;

    [SerializeField] private VisualTreeAsset m_playerUIAsset;
    
    private List<List<VisualElement>> m_boatElements;

    private List<VisualElement> m_playerElements;

    private List<List<Coroutine>> m_boatTimerLabelCoroutines;

    private List<List<string>> m_currentBoatLabels;

    private List<string> m_currentPlayerLabels;
    

    protected override void Awake()
    {
        base.Awake();
    }
    

    protected override void SetUpUI()
    {
        Debug.Log("calling set up gui in InGameHoversUI.cs");
        m_boatTimerLabelCoroutines = new List<List<Coroutine>>();

        m_boatElements = new List<List<VisualElement>>();
        
        m_playerElements = new List<VisualElement>();
        
        m_currentBoatLabels = new List<List<string>>();

        m_currentPlayerLabels = new List<string>();
        
        BoatSystem.Instance.m_onSpawnBoat += NewBoatSpawned;
        BoatSystem.Instance.m_onDeleteBoat += BoatDeleted;
        BoatSystem.Instance.m_onGoldAddedToBoat += GoldAddedToBoat;
        
        m_root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("root");

        for (int i = 0; i < PlayerSystem.Instance.m_numPlayers; i++)
        {
            m_playerElements.Add(null);
            m_currentPlayerLabels.Add("");
        }
        
        for (int teamNum = 0; teamNum < PlayerSystem.Instance.m_numTeams; teamNum++)
        {
            m_boatElements.Add(new List<VisualElement>());
            m_boatTimerLabelCoroutines.Add(new List<Coroutine>());
            m_currentBoatLabels.Add(new List<string>());
            
            for (int boatNum = 0; boatNum < BoatSystem.Instance.m_numBoatsPerTeam[teamNum]; boatNum++)
            {
                m_boatElements[teamNum].Add(null);
                m_currentBoatLabels[teamNum].Add("");
                m_boatTimerLabelCoroutines[teamNum].Add( StartCoroutine(UpdateBoatUI(teamNum, boatNum)));
            }
        }
        StartCoroutine(UpdatePlayerUIs());
    }

    void BoatDeleted(int teamNum, int boatNum)
    {
        m_boatElements[teamNum][boatNum].Clear();
        m_boatElements[teamNum][boatNum].RemoveFromHierarchy();
        m_boatElements[teamNum][boatNum] = null;
        StopCoroutine(m_boatTimerLabelCoroutines[teamNum][boatNum]);
    }

    void NewBoatSpawned(int teamNum, int boatNum)
    {
        m_boatTimerLabelCoroutines[teamNum][boatNum] = StartCoroutine(UpdateBoatUI(teamNum, boatNum));
    }

    void GoldAddedToBoat(int teamNum, int boatNum, int goldTotal, int capacity)
    {
        m_currentBoatLabels[teamNum][boatNum] = $"{goldTotal} / {capacity}";
    }

    IEnumerator UpdateBoatUI(int teamNum, int boatNum)
    {
        GameObject boat = BoatSystem.Instance.m_boatsPerTeam[teamNum][boatNum];
        BoatData boatData = boat.GetComponent<BoatData>();
        BoatTimerController boatTimerController = boat.GetComponent<BoatTimerController>();
        int initialTimeToLive = boatData.GetComponent<BoatData>().m_timeToLive;
        m_currentBoatLabels[teamNum][boatNum] = $"{boatData.m_currentTotalGoldStored}/{boatData.m_goldCapacity}";
        
        
        m_boatElements[teamNum][boatNum] = m_boatUIAsset.Instantiate();
        m_root.Add(m_boatElements[teamNum][boatNum]);

        RadialProgress timerElement = m_boatElements[teamNum][boatNum].Q<RadialProgress>("radial-timer");
        timerElement.maxTotalProgress = initialTimeToLive;

        Label capacityLabel = m_boatElements[teamNum][boatNum].Q<Label>("capacity-label");

        while (true)
        {
            Vector3 screen = Camera.main.WorldToScreenPoint(boat.transform.position);
            m_boatElements[teamNum][boatNum].style.left =
                screen.x - (m_boatElements[teamNum][boatNum].Q<RadialProgress>("radial-timer").layout.width / 2) + 50;
            m_boatElements[teamNum][boatNum].style.top = (Screen.height - screen.y) - 50;

            timerElement.progress = boatTimerController.m_currentTimeToLive;

            capacityLabel.text = m_currentBoatLabels[teamNum][boatNum];
        
            yield return null;
        }
    }

    IEnumerator UpdatePlayerUIs()
    {
        List<GameObject> players = PlayerSystem.Instance.m_players;

        List<PlayerGoldController> goldControllers = players.Select(obj => { return obj.GetComponent<PlayerGoldController>(); }).ToList();

        Label[] playerUILabels = new Label[4];

        for (int i = 0; i < PlayerSystem.Instance.m_numPlayers; i++)
        {
            m_playerElements[i] = m_playerUIAsset.Instantiate();
            m_root.Add(m_playerElements[i]);

            playerUILabels[i] = m_playerElements[i].Q<Label>("gold-count");
            m_currentPlayerLabels[i] = "0";
        }


        while (true)
        {
            for (int i = 0; i < PlayerSystem.Instance.m_numPlayers; i++)
            {
                Vector3 screen = Camera.main.WorldToScreenPoint(players[i].transform.position);
    
                m_playerElements[i].style.left =
                    screen.x - (playerUILabels[i].layout.width / 2);
                m_playerElements[i].style.top = (Screen.height - screen.y) - 100;

                m_currentPlayerLabels[i] = $"{goldControllers[i].goldCarried}";
                playerUILabels[i].text = m_currentPlayerLabels[i];
            }

            yield return null;
        }
        
    } 
}
