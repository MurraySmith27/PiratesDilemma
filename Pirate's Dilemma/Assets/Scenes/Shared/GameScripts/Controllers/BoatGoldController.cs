using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public delegate void BoatSinkEvent(int teamNum, int boatNum);

public class BoatGoldController : MonoBehaviour
{
    
    public bool m_acceptingGold = true;
    
    public GoldAddedToBoatEvent m_onGoldAddedToBoat;
    public BoatSinkEvent m_onBoatSink;

    private BoatData m_boatData;
    private BoatTimerController m_boatTimerController;

    void Start() 
    {
        m_boatData = GetComponent<BoatData>();
        m_boatTimerController = GetComponent<BoatTimerController>();

        m_boatTimerController.m_onBoatSail += OnBoatSail;

        m_acceptingGold = true;
    }

    private void OnBoatSail(int teamNum, int boatNum, List<int> goldScoredPerPlayer)
    {
        m_acceptingGold = false;
    }
    

    public void AddGold(int goldCapacity, int teamNum)
    {
        
        m_boatData.m_currentGoldStored[teamNum] += goldCapacity;
        
        if (m_boatData.m_currentTotalGoldStored > m_boatData.m_goldCapacity)
        {
            m_onBoatSink(m_boatData.m_teamNum, m_boatData.m_boatNum);
            m_acceptingGold = false;
        }
        
        m_onGoldAddedToBoat(m_boatData.m_teamNum, m_boatData.m_boatNum, m_boatData.m_currentTotalGoldStored, m_boatData.m_goldCapacity);
    }
}
