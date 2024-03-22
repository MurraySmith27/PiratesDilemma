using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SingleBombSpawner : MonoBehaviour
{
    [SerializeField] private GameObject m_bombPrefab;
    [SerializeField] private Transform m_bombSpawnPosition;

    [SerializeField]
    private float m_bombSpawnDelayTimer = 3f;
    private GameObject m_currentBomb;
    private bool m_initialized;
    private string m_singleBombStage = "Level4";

    private bool m_spawningBomb = false;

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
        List<GameObject> bombs = GameObject.FindGameObjectsWithTag("LooseBomb").ToList().Concat(GameObject.FindGameObjectsWithTag("BombInHand").ToList()).ToList();
        if (m_initialized && m_currentBomb == null && bombs.Count == 0 && SceneManager.GetActiveScene().name == m_singleBombStage && !m_spawningBomb)
        {
            StartCoroutine(SpawnBomb());
            
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

    private IEnumerator SpawnBomb()
    {
        m_spawningBomb = true;
; yield return new WaitForSeconds(m_bombSpawnDelayTimer);
        if (m_bombSpawnPosition != null && m_bombPrefab != null)
        {
            m_currentBomb = Instantiate(m_bombPrefab, m_bombSpawnPosition.position, Quaternion.identity);
            m_currentBomb.GetComponent<BombController>().SetLit(true);
        }

        m_spawningBomb = false;
    }
}