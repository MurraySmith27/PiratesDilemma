using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ItemSystem : GameSystem
{
    private static ItemSystem _instance;

    public static ItemSystem Instance
    {
        get
        {
            return _instance;
        }
    }
    
    [SerializeField] private GameObject barrelPrefab;
    
    [SerializeField] private Transform m_barrelSpawnLocation;

    [SerializeField] private float m_initialItemWaitTime;

    [SerializeField] private float m_itemSpawnInterval;

    private float m_nextItemTime;


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
    
    void Start()
    {
        m_nextItemTime = GameTimerSystem.Instance.m_gameTimerSeconds - m_initialItemWaitTime;
        GameTimerSystem.Instance.m_onGameTimerUpdate += CheckTimerForItemSpawn;
        base.SystemReady();
    }

    private void CheckTimerForItemSpawn(int currentTimerValueSeconds)
    {
        if (currentTimerValueSeconds <= m_nextItemTime)
        {
            //spawn item!
            SpawnItemRandomizer();

            m_nextItemTime -= m_itemSpawnInterval;
        }
    }
    
    void SpawnItemRandomizer()
    {
        // int randomNumberId = UnityEngine.Random.Range(0, 2);
        // 0: Barrel, 1: SuperArmor, 2: SpeedUp
        int randomNumberId = 1;
        
        if (randomNumberId == 1)
        {
            SpawnBarrel();
        }
    }
    
    
    void SpawnBarrel()
    {
        
        // Vector3[] spawnPositions = new Vector3[]
        // {
        //     new Vector3(4, 3, 8),
        //     new Vector3(8, 3, 8)
        // };
        
        // Vector3 barrelPosition = new Vector3(4, 3, 8);
        Instantiate(barrelPrefab, m_barrelSpawnLocation.position, Quaternion.identity); // Spawn barrel 
    }
    
}