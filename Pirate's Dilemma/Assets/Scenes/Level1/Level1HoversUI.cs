using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Level1UIHovers : UIBase
{

    [SerializeField] private List<GameObject> m_goldPickupZonePerTeam;
    
    [SerializeField] private List<GameObject> m_goldDropZonePerTeam;
    
    [SerializeField] private GameObject m_arrowIndicatorPrefab;
    
    [SerializeField] private float m_arrowScaleFactor = 1f;

    [SerializeField] private float m_heightAboveToHover = 0.01f;
    
    private List<GameObject> m_playerArrowIndicators = new List<GameObject>();

    void Awake()
    {
        base.Awake();
    }
    
    protected override void SetUpUI()
    {
        m_playerArrowIndicators = new List<GameObject>();
        for (int i = 0; i < PlayerSystem.Instance.m_numPlayers; i++)
        {
            m_playerArrowIndicators.Add(null);
        }
        
        GameTimerSystem.Instance.m_onGameStart += OnGameStart;
        PlayerSystem.Instance.m_onPlayerPickupGold += OnGoldPickedUp;
        PlayerSystem.Instance.m_onPlayerDropGold += OnGoldDropped;
    }

    void OnDestroy()
    {
        GameTimerSystem.Instance.m_onGameStart -= OnGameStart;
        PlayerSystem.Instance.m_onPlayerPickupGold -= OnGoldPickedUp;
        PlayerSystem.Instance.m_onPlayerDropGold -= OnGoldDropped;
    }

    private void OnGameStart()
    {
        //at the start of this level, players 2 and 4 should start with arrows to their respective gold piles
        m_playerArrowIndicators[2] = Instantiate(m_arrowIndicatorPrefab, Vector3.zero, Quaternion.identity);
        
        m_playerArrowIndicators[2].GetComponent<ArrowIndicatorController>().StartArrowIndicatorController(m_heightAboveToHover,
            PlayerSystem.Instance.m_teamColors[0],
            scaleFactor: m_arrowScaleFactor,
            objectToTrack: PlayerSystem.Instance.m_players[2],
            objectToPointTo: m_goldPickupZonePerTeam[0],
            camera: Camera.main
            );
        
        m_playerArrowIndicators[3] = Instantiate(m_arrowIndicatorPrefab, Vector3.zero, Quaternion.identity);
        
        m_playerArrowIndicators[3].GetComponent<ArrowIndicatorController>().StartArrowIndicatorController(m_heightAboveToHover,
            PlayerSystem.Instance.m_teamColors[1],
            scaleFactor: m_arrowScaleFactor,
            objectToTrack: PlayerSystem.Instance.m_players[3],
            objectToPointTo: m_goldPickupZonePerTeam[1],
            camera: Camera.main
        );
    }

    private void OnGoldPickedUp(int teamNum, int playerNum) 
    {

        if (playerNum == 1)
        {
            m_playerArrowIndicators[0] = Instantiate(m_arrowIndicatorPrefab, Vector3.zero, Quaternion.identity);
        
            m_playerArrowIndicators[0].GetComponent<ArrowIndicatorController>().StartArrowIndicatorController(m_heightAboveToHover,
                PlayerSystem.Instance.m_teamColors[0],
                scaleFactor: m_arrowScaleFactor,
                objectToTrack: PlayerSystem.Instance.m_players[0],
                objectToPointTo: m_goldDropZonePerTeam[0],
                camera: Camera.main
            );
        }
        else if (playerNum == 2)
        {
            m_playerArrowIndicators[1] = Instantiate(m_arrowIndicatorPrefab, Vector3.zero, Quaternion.identity);
        
            m_playerArrowIndicators[1].GetComponent<ArrowIndicatorController>().StartArrowIndicatorController(m_heightAboveToHover,
                PlayerSystem.Instance.m_teamColors[1],
                scaleFactor: m_arrowScaleFactor,
                objectToTrack: PlayerSystem.Instance.m_players[1],
                objectToPointTo: m_goldDropZonePerTeam[1],
                camera: Camera.main
            );
        }
        else if (playerNum == 3)
        {
            m_playerArrowIndicators[2].GetComponent<ArrowIndicatorController>().m_objectToPointTo = PlayerSystem.Instance.m_players[0];
        }
        else if (playerNum == 4)
        {
            m_playerArrowIndicators[3].GetComponent<ArrowIndicatorController>().m_objectToPointTo = PlayerSystem.Instance.m_players[1];
        }
    }

    private void OnGoldDropped(int teamNum, int playerNum)
    {
        if (playerNum == 1)
        {
            m_playerArrowIndicators[0].GetComponent<ArrowIndicatorController>().StopArrowIndicatorController();
        }
        else if (playerNum == 2)
        {
            m_playerArrowIndicators[1].GetComponent<ArrowIndicatorController>().StopArrowIndicatorController();
        }
        else if (playerNum == 3)
        {
            m_playerArrowIndicators[2].GetComponent<ArrowIndicatorController>().m_objectToPointTo = m_goldPickupZonePerTeam[0];
        }
        else if (playerNum == 4)
        {
            m_playerArrowIndicators[3].GetComponent<ArrowIndicatorController>().m_objectToPointTo =
                m_goldPickupZonePerTeam[1];
        }
    }
}
