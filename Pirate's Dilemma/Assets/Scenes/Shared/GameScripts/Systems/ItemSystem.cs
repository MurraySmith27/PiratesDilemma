using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ItemSystem : GameSystem
{
    public GameObject barrelPrefab;
    
    [SerializeField] private Vector3 m_barrelSpawnLocation;

    [SerializeField] private float m_initialItemWaitTime;
    
    
    void Start()
    {
        StartCoroutine(SpawnItemsWithDelay(1));
    }
    
    IEnumerator SpawnItemsWithDelay(int itemCount)  
    {
        yield return new WaitForSeconds(m_initialItemWaitTime);
        for (int i = 0; i < itemCount; i++)
        {
            
            SpawnItemRandomizer(); 

            yield return new WaitForSeconds(UnityEngine.Random.Range(20f, 30f));
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
        
        Instantiate(barrelPrefab, m_barrelSpawnLocation, Quaternion.identity); // Spawn barrel 
        
    }
    
}