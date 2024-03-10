using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void LooseBombCollisionEvent();
public class LooseBombController : MonoBehaviour
{
    [SerializeField] private float m_bombAliveSeconds = 5f;
    
    [SerializeField] private GameObject m_explosionPrefab;
    
    [SerializeField] private float m_explosionAliveSeconds;
    
    public LooseBombCollisionEvent m_onLooseBombCollision;

    public int m_lastHeldTeamNum;

    public int m_damagePerBomb = 1;

    public bool m_isLit = false;

    
    void Start()
    {
        StartCoroutine(DespawnAfterSeconds());
    }

    IEnumerator DespawnAfterSeconds()
    {
        yield return new WaitForSeconds(m_bombAliveSeconds);
        Destroy(this.gameObject);
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.layer == LayerMask.NameToLayer("Boat") && collider.gameObject.GetComponent<BoatData>().m_teamNum != m_lastHeldTeamNum && m_isLit)
        {
            //landed in drop zone, score for team. 
            BoatDamageController boatDamageController = collider.gameObject.GetComponent<BoatDamageController>();
            if (boatDamageController.m_acceptingDamage)
            {
                boatDamageController.TakeDamage(m_damagePerBomb);
            }

            GenerateExplosion(transform.position);
            Destroy(this.gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {   
        GetComponent<TrailRenderer>().enabled = false;
        if (collision.gameObject.layer != LayerMask.NameToLayer("Player"))
        {
            if (m_onLooseBombCollision != null && m_onLooseBombCollision.GetInvocationList().Length > 0)
            {
                m_onLooseBombCollision();
            }
        }

        if (m_isLit)
        {
            GenerateExplosion(collision.contacts[0].point);
        }
    }

    private void GenerateExplosion(Vector3 explosionCenter)
    {
        Debug.Log("generating explosion!");
        GameObject explosion = Instantiate(m_explosionPrefab, explosionCenter, Quaternion.identity);
        Destroy(explosion, m_explosionAliveSeconds);
    }
}
