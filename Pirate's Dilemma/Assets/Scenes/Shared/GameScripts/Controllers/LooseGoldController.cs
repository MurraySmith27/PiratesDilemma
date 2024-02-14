using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void LooseGoldCollisionEvent();
public class LooseGoldController : MonoBehaviour
{
    public LooseGoldCollisionEvent m_onLooseGoldCollision;

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
        
        if (collider.gameObject.layer == LayerMask.NameToLayer("GoldDropZone"))
        {
            //landed in drop zone, score for team. 
            GameObject boat = collider.gameObject.GetComponent<GoldDropZoneData>().m_boat;
            BoatGoldController boatGoldController = boat.GetComponent<BoatGoldController>();
            if (boatGoldController.m_acceptingGold)
            {
                boatGoldController.AddGold(1, boat.GetComponent<BoatData>().m_teamNum);
            }
            Destroy(this.gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("Player"))
        {
            Debug.Log($"loose gold colliding. object: {collision.gameObject.name}");
            if (m_onLooseGoldCollision.GetInvocationList().Length > 0)
            {
                m_onLooseGoldCollision();
            }
        }
    }
}
