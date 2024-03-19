using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BombSpawner : MonoBehaviour
{
    [SerializeField] private GameObject m_bombPrefab;
    
    [SerializeField] private List<Transform> m_bombSpawnPositions;
    
    [SerializeField] private float m_bombSpawnInterval;

    private List<GameObject> m_spawnedBombs;

    private int m_lastBombSpawnedTimeStamp;
    
    private bool m_initialized;

    private Coroutine m_simulateTimerUpdateCoroutine;
    
    void Start()
    {
        m_spawnedBombs = new List<GameObject>();

        foreach (Transform position in m_bombSpawnPositions)
        {
            m_spawnedBombs.Add(null);
        }
        
        m_initialized = false;

        m_lastBombSpawnedTimeStamp = -1;
        
        GameTimerSystem.Instance.m_onGameStart += OnGameStart;
        GameTimerSystem.Instance.m_onGameFinish += OnGameFinish;

        //for charcter select so bombs spawn.
        if (SceneManager.GetActiveScene().name == GameTimerSystem.Instance.m_characterSelectSceneName)
        {
            m_simulateTimerUpdateCoroutine = StartCoroutine(SimulateTimerUpdate());
        }
    }

    void OnDestroy()
    {
        GameTimerSystem.Instance.m_onGameStart -= OnGameStart;
        GameTimerSystem.Instance.m_onGameFinish -= OnGameFinish;
    }

    private void OnGameStart()
    {
        if (m_simulateTimerUpdateCoroutine != null)
        {
            StopCoroutine(m_simulateTimerUpdateCoroutine);
        }
        m_initialized = true;
        m_lastBombSpawnedTimeStamp = -1;
        GameTimerSystem.Instance.m_onGameTimerUpdate += OnTimerUpdate;
    }
    
    private IEnumerator SimulateTimerUpdate()
    {
        
        //first spawn all bombs
        while (TrySpawnBomb())
        {
            
        }
        
        int count = 0;
        
        while (true)
        {
            OnTimerUpdate(count++);
            yield return new WaitForSeconds(1f);
            
        }
    }

    private void OnGameFinish()
    {
        m_initialized = false;
        GameTimerSystem.Instance.m_onGameTimerUpdate -= OnTimerUpdate;
    }

    private void OnTimerUpdate(int newTimerSeconds)
    {
        if (m_lastBombSpawnedTimeStamp == -1 || newTimerSeconds - m_lastBombSpawnedTimeStamp >= m_bombSpawnInterval)
        {
            if (TrySpawnBomb())
            {
                m_lastBombSpawnedTimeStamp = newTimerSeconds;
            }
        }
    }
    
    private bool TrySpawnBomb()
    {
        for (int i = 0; i < m_bombSpawnPositions.Count; i++)
        {
            if (m_spawnedBombs[i] == null)
            {
                m_spawnedBombs[i] = Instantiate(m_bombPrefab, m_bombSpawnPositions[i].position, Quaternion.identity);
                return true;
            }
        }

        return false;
    }
}