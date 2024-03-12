using System.Collections.Generic;
using System.Linq;
using FMODUnity;
using UnityEngine;

public class BoatData : MonoBehaviour
{
    // The team that this boat belongs to.
    public int m_teamNum;
    // The boat number within the team.
    public int m_boatNum;
    // The amount of remaining health the boat has.
    public int m_remainingHealth;
    // The total health the boat has.
    public int m_maxHealth;
    //The sink audio source
    public StudioEventEmitter m_sinkEventEmitter;
}