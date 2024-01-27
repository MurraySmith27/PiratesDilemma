using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MyUILibrary;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class InGameUIHoversController : MonoBehaviour
{
    private VisualElement m_root;

    [SerializeField] private VisualTreeAsset m_boatUIAsset;

    [SerializeField] private VisualTreeAsset m_playerUIAsset;

    private List<GameObject> m_boats;

    private List<VisualElement> m_boatElements;

    private List<VisualElement> m_playerElements;

    private List<Coroutine> m_timerLabelCoroutines;

    private List<string> m_currentBoatLabels;

    private List<string> m_currentPlayerLabels;

    void Start()
    {
        m_timerLabelCoroutines = new List<Coroutine>();

        m_boatElements = new List<VisualElement>();
        
        m_playerElements = new List<VisualElement>();

        m_boats = new List<GameObject>();

        m_currentBoatLabels = new List<string>();

        m_currentPlayerLabels = new List<string>();
        
        BoatController.OnSpawnBoat += NewBoatSpawned;
        BoatController.OnAddGold += GoldAddedToBoat;
        
        m_root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("root");

        for (int i = 0; i < PlayerSystem.Instance.m_numPlayers; i++)
        {
            m_playerElements.Add(null);
            m_currentPlayerLabels.Add("");
        }
        
        for (int i = 0; i < BoatSystem.Instance.m_numBoats; i++)
        {
            m_boats.Add(null);
            m_boatElements.Add(null);
            m_timerLabelCoroutines[i] = StartCoroutine(UpdateBoatUI(i));
            m_currentBoatLabels.Add("");
        }
        StartCoroutine(UpdatePlayerUIs());
    }

    void BoatDeleted(int boatNum)
    {
        m_boatElements[boatNum].Clear();
        m_boatElements[boatNum].RemoveFromHierarchy();
        m_boatElements[boatNum] = null;
        StopCoroutine(m_timerLabelCoroutines[boatNum]);
    }

    void NewBoatSpawned(int boatNum)
    {
        m_timerLabelCoroutines[boatNum] = StartCoroutine(UpdateBoatUI(boatNum));
    }

    void GoldAddedToBoat(int boatNum, int goldTotal, int capacity)
    {
        m_currentBoatLabels[boatNum] = $"{goldTotal} / {capacity}";
    }

    IEnumerator UpdateBoatUI(int boatNum)
    {
        int initialTimeToLive = 0;
        yield return null;
        while (m_boats[boatNum] == null)
        {
            foreach (GameObject boat in GameObject.FindGameObjectsWithTag("Boat"))
            {
                if (boat.GetComponent<BoatController>().boatSlot == boatNum)
                {
                    m_boats[boatNum] = boat;
                }
            }
            
            if (m_boats[boatNum])
            {
                BoatController boatController = m_boats[boatNum].GetComponent<BoatController>();
                initialTimeToLive = boatController.timeToLive;
                boatController.OnDeleteBoat += BoatDeleted;
                m_currentBoatLabels[boatNum] = $"0/{boatController.boatTotalCapacity}";
            }

            yield return null;
        }
        
        m_boatElements[boatNum] = m_boatUIAsset.Instantiate();
        m_root.Add(m_boatElements[boatNum]);

        RadialProgress timerElement = m_boatElements[boatNum].Q<RadialProgress>("radial-timer");
        timerElement.maxTotalProgress = initialTimeToLive;

        Label capacityLabel = m_boatElements[boatNum].Q<Label>("capacity-label");

        while (true)
        {
            if (m_boatElements[boatNum] != null)
            {
                Vector3 screen = Camera.main.WorldToScreenPoint(m_boats[boatNum].transform.position);
                m_boatElements[boatNum].style.left =
                    screen.x - (m_boatElements[boatNum].Q<RadialProgress>("radial-timer").layout.width / 2) + 50;
                m_boatElements[boatNum].style.top = (Screen.height - screen.y) - 50;

                BoatController boatController = m_boats[boatNum].GetComponent<BoatController>();
                timerElement.progress = boatController.timeToLive;

                capacityLabel.text = m_currentBoatLabels[boatNum];
            }
            
            yield return null;
        }
    }

    IEnumerator UpdatePlayerUIs()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        GoldController[] goldControllers = players.Select(obj => { return obj.GetComponent<GoldController>(); }).ToArray();

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
                //Begin hack
                players = GameObject.FindGameObjectsWithTag("Player");
                goldControllers = players.Select(obj => { return obj.GetComponent<GoldController>(); }).ToArray();

                if (m_playerElements[i] == null)
                {
                    m_playerElements[i] = m_playerUIAsset.Instantiate();
                    m_root.Add(m_playerElements[i]);
                    playerUILabels[i] = m_playerElements[i].Q<Label>("gold-count");
                    m_currentPlayerLabels[i] = "0";
                }
                
                //end hack
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
