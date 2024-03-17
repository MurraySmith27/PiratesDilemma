using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;


public delegate void BoatSinkEvent(int teamNum, int boatNum);
// public delegate void BoatSailEvent(int teamNum, int boatNum, List<int> goldScoredPerTeam);

public class BoatDamageController : MonoBehaviour
{
    
    [FormerlySerializedAs("m_acceptingGold")] [HideInInspector] public bool m_acceptingDamage = true;
    // public GameObject m_goldDropZone;
    
    public BoatDamagedEvent m_onBoatDamaged;
    public BoatSinkEvent m_onBoatSink;
    // public BoatSailEvent m_onBoatSail;

    private BoatData m_boatData;

    void Start() 
    {
        m_boatData = GetComponent<BoatData>();

        m_acceptingDamage = true;
    }

    public void TakeDamage(int damage)
    {
        m_boatData.m_remainingHealth = Math.Max(0, m_boatData.m_remainingHealth - damage);
        
        m_onBoatDamaged(m_boatData.m_teamNum, m_boatData.m_boatNum, damage, m_boatData.m_remainingHealth, m_boatData.m_maxHealth);
        
        if (m_boatData.m_remainingHealth == 0)
        {
            m_onBoatSink(m_boatData.m_teamNum, m_boatData.m_boatNum);
            m_acceptingDamage = false;
        }
    }

    private void OnTriggerEnter(Collider collider)
    {

        ExplosionController explosionController = collider.gameObject.GetComponent<ExplosionController>();
        if (collider.gameObject.layer == LayerMask.NameToLayer("Explosion") && explosionController.m_teamNum != m_boatData.m_teamNum)
        {
            if (m_acceptingDamage)
            {
                TakeDamage(explosionController.m_boatDamage);
            }
        }
    }
}
