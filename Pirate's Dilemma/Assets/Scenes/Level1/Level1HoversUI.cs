using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level1UIHovers : UIBase
{

    [SerializeField] private List<GameObject> m_goldPickupZonesPerTeam;
    
    [SerializeField] private GameObject m_arrowIndicatorPrefab;
    
    [SerializeField] private float m_arrowScaleFactor = 1f;
    
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
        
        
        //PlayerSystem.Instance.m_onPlayerPickupGold += OnGoldPickedUp;
    }

    void OnDestroy()
    {
        GameTimerSystem.Instance.m_onGameStart -= OnGameStart;
        //PlayerSystem.Instance.m_onPlayerPickupGold -= OnGoldPickedUp;
    }


    private void OnGameStart()
    {
        //at the start of this level, players 2 and 4 should start with arrows to their respective gold piles
        m_playerArrowIndicators[2] = Instantiate(m_arrowIndicatorPrefab, Vector3.zero, Quaternion.identity);
        
        m_playerArrowIndicators[2].GetComponent<ArrowIndicatorController>().StartArrowIndicatorController(0.01f,
            PlayerSystem.Instance.m_teamColors[0],
            scaleFactor: m_arrowScaleFactor,
            objectToTrack: PlayerSystem.Instance.m_players[2],
            objectToPointTo: m_goldPickupZonesPerTeam[0],
            camera: Camera.main
            );
        
        m_playerArrowIndicators[3] = Instantiate(m_arrowIndicatorPrefab, Vector3.zero, Quaternion.identity);
        
        m_playerArrowIndicators[3].GetComponent<ArrowIndicatorController>().StartArrowIndicatorController(0.01f,
            PlayerSystem.Instance.m_teamColors[1],
            scaleFactor: m_arrowScaleFactor,
            objectToTrack: PlayerSystem.Instance.m_players[3],
            objectToPointTo: m_goldPickupZonesPerTeam[1],
            camera: Camera.main
        );
    }
}
