
using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;


public class CameraController : MonoBehaviour
{
    public CinemachineVirtualCamera m_vcam;
    public CinemachineVirtualCamera m_topLevelCam;

    private List<Vector3> initialPlayerPositions = new List<Vector3>();
    
    private CinemachineBasicMultiChannelPerlin m_vcamPerlinShake;

    void Start()
    {
        GameTimerSystem.Instance.m_onReadyToStartGameTimer += ActivateDeactivateVCam;
        CaptureInitialPlayerPositions();
    }

    private void OnDestroy()
    {
        GameTimerSystem.Instance.m_onReadyToStartGameTimer -= ActivateDeactivateVCam;
    }
    
    void CaptureInitialPlayerPositions()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            initialPlayerPositions.Add(player.transform.position);
        }
    }

    private void Update()
    {
    
        Vector3 averagePosition = CalculateAveragePosition();
        if (m_vcam.LookAt != null)
        {
            m_vcam.LookAt.position = new Vector3(averagePosition.x, m_vcam.LookAt.position.y, m_vcam.LookAt.position.z);
            m_vcam.Follow.position = new Vector3(averagePosition.x, m_vcam.Follow.position.y, m_vcam.Follow.position.z);
        }
    }
    
    Vector3 CalculateAveragePosition()
    {

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        Vector3 sumPosition = Vector3.zero;
        int count = players.Length;

        if (count == 0)
        {
            return sumPosition; 
        }
        
        // foreach (GameObject player in players)
        // {
        //     sumPosition += player.transform.position;
        // }

        foreach (Vector3 initialPos in initialPlayerPositions)
        {
            sumPosition += new Vector3(initialPos.x, 0, 0);
            count += 1;
            sumPosition += new Vector3(initialPos.x, 0, 0);
            count += 1;
            sumPosition += new Vector3(initialPos.x, 0, 0);
            count += 1;
            sumPosition += new Vector3(initialPos.x, 0, 0);
            count += 1;
            sumPosition += new Vector3(initialPos.x, 0, 0);
            count += 1;
            sumPosition += new Vector3(initialPos.x, 0, 0);
            count += 1;
            
        }
        
        foreach (GameObject player in players)
        {
            sumPosition += new Vector3(player.transform.position.x, 0, 0);
        }
        return sumPosition / count; 
    }

    void ActivateDeactivateVCam()
    {
        // yield return new WaitForSeconds(GameTimerSystem.Instance.m_gameSceneLoadedBufferSeconds);
        
        m_vcam.gameObject.SetActive(false);
        m_vcam.gameObject.SetActive(true);

        
        m_vcamPerlinShake = m_vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    }
    
    public void StartScreenShake()
    {
        m_vcamPerlinShake.m_AmplitudeGain = 1;
    }

    public void StopScreenShake()
    {
        m_vcamPerlinShake.m_AmplitudeGain = 0;
    }
}