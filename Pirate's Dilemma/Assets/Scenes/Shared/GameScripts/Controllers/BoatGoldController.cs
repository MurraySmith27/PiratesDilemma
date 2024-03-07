using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public delegate void BoatSinkEvent(int teamNum, int boatNum);
public delegate void BoatSailEvent(int teamNum, int boatNum, List<int> goldScoredPerTeam);

public class BoatGoldController : MonoBehaviour
{
    
    [HideInInspector] public bool m_acceptingGold = true;
    public GameObject m_goldDropZone;
    
    public GoldAddedToBoatEvent m_onGoldAddedToBoat;
    public BoatSinkEvent m_onBoatSink;
    public BoatSailEvent m_onBoatSail;

    private BoatData m_boatData;

    void Start() 
    {
        m_boatData = GetComponent<BoatData>();

        m_acceptingGold = true;
    }

    private void OnBoatSail(int teamNum, int boatNum, List<int> goldScoredPerPlayer)
    {
        m_acceptingGold = false;
    }

    public void BoardPlayerOnBoat(Transform playerTransform)
    {
        playerTransform.parent = m_boatData.m_playerBoardedPositions[m_boatData.m_numPlayersBoarded];
        m_boatData.m_numPlayersBoarded++;
        playerTransform.localPosition = new Vector3(0, 0, 0);
        
        m_onBoatSail(m_boatData.m_teamNum, m_boatData.m_boatNum, m_boatData.m_currentGoldStored);
        m_boatData.m_numPlayersBoarded = 0;
    }

    public void DismountPlayerFromBoat(Transform playerTransform, Transform newParent, Vector3 playerRespawnPosition, bool useDefaultPosition)
    {
        if (useDefaultPosition)
        {
            playerRespawnPosition = m_goldDropZone.transform.position + new Vector3(0, 3f, 0);
        }
        playerTransform.gameObject.GetComponent<PlayerMovementController>().WarpToPosition(playerRespawnPosition);
        playerTransform.parent = newParent;

        // m_boatData.m_numPlayersBoarded--;
    }
    

    public void AddGold(int goldAdded, int teamNum)
    {
        m_boatData.m_currentGoldStored[teamNum - 1] += goldAdded;
        
        if (m_boatData.m_currentTotalGoldStored > m_boatData.m_goldCapacity)
        {
            m_onBoatSink(m_boatData.m_teamNum, m_boatData.m_boatNum);
            m_acceptingGold = false;
        }
        
        m_onGoldAddedToBoat(m_boatData.m_teamNum, m_boatData.m_boatNum, m_boatData.m_currentTotalGoldStored, m_boatData.m_goldCapacity);
    }
}
