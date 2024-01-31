using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoatData : MonoBehaviour
{
    // The team that this boat belongs to.
    public int m_teamNum;
    // The boat number within the team.
    public int m_boatNum;
    // The amount of total gold stored on the boat between every team.
    public int m_currentTotalGoldStored
    {
        get
        {
            return m_currentGoldStored.Sum();
        }
    }

    // The amount of gold currently stored on the boat for each team.
    public List<int> m_currentGoldStored;
    // The total capacity of gold that this boat can hold
    public int m_goldCapacity;
    // The total time to live for this boat (when it's spawned, not the current timer value).
    public int m_timeToLive;
    //The sink audio source
    public AudioSource m_sinkAudioSource;
    // The sail away audio source.
    public AudioSource m_sailAudioSource;
    private void Awake()
    {
        m_currentGoldStored = new List<int>();

        for (int i = 0; i < PlayerSystem.Instance.m_numTeams; i++)
        {
            m_currentGoldStored.Add(0);
        }
    }
}