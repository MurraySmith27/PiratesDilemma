using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SingleBombSpawner : MonoBehaviour
{
    [SerializeField] private GameObject m_bombPrefab;
    [SerializeField] private Transform m_bombSpawnPosition;
    private GameObject m_currentBomb;
    private bool m_initialized;
    private string m_singleBombStage = "Level4"; 

    void Start()
    {
        m_initialized = false;
        
        GameTimerSystem.Instance.m_onGameStart += OnGameStart;
        GameTimerSystem.Instance.m_onGameFinish += OnGameFinish;
        
        if (SceneManager.GetActiveScene().name == m_singleBombStage)
        {
            SpawnBomb();
        }
    }

    void OnDestroy()
    {
        GameTimerSystem.Instance.m_onGameStart -= OnGameStart;
        GameTimerSystem.Instance.m_onGameFinish -= OnGameFinish;
    }

    private void Update()
    {
        if (m_initialized && m_currentBomb == null && SceneManager.GetActiveScene().name == m_singleBombStage)
        {
            SpawnBomb();
        }
    }

    private void OnGameStart()
    {
        m_initialized = true;
    }

    private void OnGameFinish()
    {
        m_initialized = false;
    }

    private void SpawnBomb()
    {
        if (m_bombSpawnPosition != null && m_bombPrefab != null)
        {
            m_currentBomb = Instantiate(m_bombPrefab, m_bombSpawnPosition.position, Quaternion.identity);
        }
    }
}