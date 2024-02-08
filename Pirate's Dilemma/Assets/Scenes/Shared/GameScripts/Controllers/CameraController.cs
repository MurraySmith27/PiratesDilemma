
using Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private CinemachineVirtualCamera m_vcam;

    private CinemachineBasicMultiChannelPerlin m_vcamPerlinShake;

    void Awake()
    {
        m_vcam = GetComponent<CinemachineVirtualCamera>();
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