using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class BackgroundSoundController : MonoBehaviour
{
    [SerializeField] private StudioEventEmitter m_oceanAmbienceEventEmitter;

    [SerializeField] private StudioEventEmitter m_backgroundMusic;

    [SerializeField] private StudioEventEmitter m_shoutSound;

    [SerializeField] private float m_shoutSoundVolume = 1f;
    
    [SerializeField] private float m_backgroundMusicBeforeReadyUpVolume = 1f;

    [SerializeField] private float m_backgroundMusicAfterReadyUpVolume = 1f;
    
    [SerializeField] private float m_oceanAmbienceVolume = 1f;

    private bool m_dynamicMusicState = false;

    private bool m_backgroundMusicPlaying = false;

    private float m_currentMusicVolume;

    void Start()
    {
        
        m_oceanAmbienceEventEmitter.Play();
        m_oceanAmbienceEventEmitter.EventInstance.setVolume(m_oceanAmbienceVolume);
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("Space", 1);

        //todo: remove this when starting from main menu
        OnCharacterSelectStart();
        
        PlayerSystem.Instance.m_onPlayerReadyUpToggle += UpdateDynamicCharacterSelectMusic;
        
        GameTimerSystem.Instance.m_onCharacterSelectStart += OnCharacterSelectStart;
        
        GameTimerSystem.Instance.m_onCharacterSelectEnd += OnCharacterSelectEnd;
        
        GameTimerSystem.Instance.m_onLevelSelectStart += OnLevelSelectStart;

        GameTimerSystem.Instance.m_onLevelSelectEnd += OnLevelSelectEnd;

        GameTimerSystem.Instance.m_onLevelEndSceneStart += OnLevelEndSceneStart;
        
        
        DontDestroyOnLoad(this.gameObject);
    }

    void OnDestroy()
    {
        GameTimerSystem.Instance.m_onCharacterSelectStart -= OnCharacterSelectStart;
        
        GameTimerSystem.Instance.m_onCharacterSelectEnd -= OnCharacterSelectEnd;
        
        GameTimerSystem.Instance.m_onLevelSelectStart -= OnLevelSelectStart;

        GameTimerSystem.Instance.m_onLevelSelectEnd -= OnLevelSelectEnd;
        
        PlayerSystem.Instance.m_onPlayerReadyUpToggle -= UpdateDynamicCharacterSelectMusic;
    }

    void Update()
    {
        m_oceanAmbienceEventEmitter.EventInstance.setVolume(m_oceanAmbienceVolume);
        if (m_backgroundMusicPlaying)
        {
            m_backgroundMusic.EventInstance.setVolume(m_currentMusicVolume);
        }
    }

    private void UpdateDynamicCharacterSelectMusic(int playerNum, bool isReady)
    {
        foreach (bool ready in PlayerSystem.Instance.m_readyPlayers)
        {
            if (!ready)
            {
                
                m_dynamicMusicState = false;
                m_backgroundMusic.EventInstance.setParameterByName("MusicShift", 0f);
                m_currentMusicVolume = m_backgroundMusicBeforeReadyUpVolume;
                return;
            }
        }
        
        m_dynamicMusicState = true;
        m_backgroundMusic.EventInstance.setParameterByName("MusicShift", 1f);
        m_currentMusicVolume = m_backgroundMusicAfterReadyUpVolume;
    }

    private void OnCharacterSelectStart()
    {
        m_backgroundMusic.Play();
        m_backgroundMusicPlaying = true;
        m_backgroundMusic.EventInstance.setVolume(m_backgroundMusicBeforeReadyUpVolume);
        m_dynamicMusicState = false;
        m_currentMusicVolume = m_backgroundMusicBeforeReadyUpVolume;
    }
    private void OnCharacterSelectEnd()
    {
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("Space", 0);
    }

    private void OnLevelSelectStart()
    {
        if (!m_backgroundMusicPlaying)
        {
            m_backgroundMusic.Play();
            m_dynamicMusicState = true;
            m_backgroundMusic.EventInstance.setParameterByName("MusicShift", 1f);
            m_currentMusicVolume = m_backgroundMusicAfterReadyUpVolume;
            m_backgroundMusicPlaying = true;
        }
    }

    private void OnLevelSelectEnd()
    {
        if (m_backgroundMusicPlaying)
        {
            m_dynamicMusicState = false;
            m_backgroundMusic.EventInstance.setParameterByName("MusicShift", 0f);
            m_currentMusicVolume = m_backgroundMusicBeforeReadyUpVolume;
            m_backgroundMusic.Stop();
            m_backgroundMusicPlaying = false;
            m_shoutSound.Play();
        }
    }

    private void OnLevelEndSceneStart()
    {
        if (!m_backgroundMusicPlaying)
        {
            m_backgroundMusic.Play();
            m_dynamicMusicState = true;
            m_backgroundMusic.EventInstance.setParameterByName("MusicShift", 1f);
            m_currentMusicVolume = m_backgroundMusicAfterReadyUpVolume;
            m_backgroundMusicPlaying = true;
        }

        LevelEndSceneController.Instance.m_onImpactBackgroundAppear += OnLevelEndSceneImpactBackgroundAppear;
    }

    private void OnLevelEndSceneImpactBackgroundAppear()
    {
        m_dynamicMusicState = false;
        m_backgroundMusic.EventInstance.setParameterByName("MusicShift", 0f);
        m_currentMusicVolume = m_backgroundMusicBeforeReadyUpVolume;
        m_backgroundMusic.Stop();
        m_backgroundMusicPlaying = false;
        m_shoutSound.Play();
    }

}
