using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class CharacterSelectSoundController : MonoBehaviour
{
    [SerializeField] private StudioEventEmitter m_projectorSFXEmitter;

    [SerializeField] private float m_projectorSFXVolume = 1f; 
    void Start()
    {
        GameTimerSystem.Instance.m_onCharacterSelectEnd += SpinDownProjector;
        m_projectorSFXEmitter.EventInstance.setVolume(m_projectorSFXVolume);
        m_projectorSFXEmitter.Play();
    }

    void Update()
    {
        m_projectorSFXEmitter.EventInstance.setVolume(m_projectorSFXVolume);
    }

    void SpinDownProjector()
    {
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("Out", 1);
    }
}