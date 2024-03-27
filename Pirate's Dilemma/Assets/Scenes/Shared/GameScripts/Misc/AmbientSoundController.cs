using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class AmbientSoundController : MonoBehaviour
{
    [SerializeField] private StudioEventEmitter m_oceanAmbienceEventEmitter;
 
    [SerializeField] private float m_oceanAmbienceVolume = 1f;

    void Start()
    {
        
        m_oceanAmbienceEventEmitter.Play();
        m_oceanAmbienceEventEmitter.EventInstance.setVolume(m_oceanAmbienceVolume);
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("Space", 1);

        GameTimerSystem.Instance.m_onCharacterSelectEnd += SpinUpAmbientNoise;
        
        DontDestroyOnLoad(this.gameObject);
    }

    void Update()
    {
        m_oceanAmbienceEventEmitter.EventInstance.setVolume(m_oceanAmbienceVolume);
    }

    private void SpinUpAmbientNoise()
    {
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("Space", 0);
    }
}
