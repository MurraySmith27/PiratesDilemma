using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void LooseGoldCollisionEvent();
public class LooseGoldController : MonoBehaviour
{
    public LooseGoldCollisionEvent m_onLooseGoldCollision;

    public int m_lastHeldTeamNum;
    
    void Start()
    {
        StartCoroutine(DespawnAfterSeconds());
    }

    IEnumerator DespawnAfterSeconds()
    {
        yield return new WaitForSeconds(5);
        Destroy(this.gameObject);
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.layer == LayerMask.NameToLayer("Boat") && collider.gameObject.GetComponent<BoatData>().m_teamNum != m_lastHeldTeamNum)
        {
            //landed in drop zone, score for team. 
            BoatGoldController boatGoldController = collider.gameObject.GetComponent<BoatGoldController>();
            if (boatGoldController.m_acceptingGold)
            {
                boatGoldController.AddGold(1, collider.gameObject.GetComponent<BoatData>().m_teamNum);
            }
            Destroy(this.gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {   
        GetComponent<TrailRenderer>().enabled = false;
        if (collision.gameObject.layer != LayerMask.NameToLayer("Player"))
        {
            if (m_onLooseGoldCollision.GetInvocationList().Length > 0)
            {
                m_onLooseGoldCollision();
            }
        }
    }
}
