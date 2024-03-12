using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void LooseBombCollisionEvent();
public class BombController : MonoBehaviour
{
    [SerializeField] private float m_bombAliveSeconds = 5f;
    
    [SerializeField] private GameObject m_explosionPrefab;
    
    [SerializeField] private float m_explosionAliveSeconds;

    [SerializeField] private float m_explosionAnimationSeconds = 2f;

    [SerializeField] private GameObject m_fuseFireParticle;

    [SerializeField] private bool m_isLoose = false;

    [SerializeField] private AudioSource m_explosionAudioSource;
    
    [SerializeField] private AudioSource m_hissAudioSource;
    
    public LooseBombCollisionEvent m_onLooseBombCollision;

    public int m_lastHeldTeamNum;

    public int m_damagePerBomb = 1;

    private bool m_isLit = false;

    private Coroutine m_despawnCoroutine;
    
    void Start()
    {
        m_despawnCoroutine = StartCoroutine(DespawnAfterSeconds());
    }

    IEnumerator DespawnAfterSeconds()
    {
        yield return new WaitForSeconds(m_bombAliveSeconds);
        Destroy(this.gameObject);
    }

    public void SetLit(bool isLit)
    {
        m_isLit = isLit;

        m_fuseFireParticle.SetActive(isLit);

        if (isLit)
        {
            m_hissAudioSource.Play();
        }
        else
        {
            m_hissAudioSource.Stop();
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.layer == LayerMask.NameToLayer("Boat") && collider.gameObject.GetComponent<BoatData>().m_teamNum != m_lastHeldTeamNum && m_isLit)
        {
            //landed in drop zone, score for team. 
            BoatDamageController boatDamageController = collider.gameObject.GetComponent<BoatDamageController>();
            if (boatDamageController.m_acceptingDamage)
            {
                
                StartCoroutine(GenerateExplosion(transform.position));
                boatDamageController.TakeDamage(m_damagePerBomb);
            }
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
            StartCoroutine(GenerateExplosion(collision.contacts[0].point));
            
        }
    
    }

    private IEnumerator GenerateExplosion(Vector3 explosionCenter)
    {
        if (m_despawnCoroutine != null)
        {
            StopCoroutine(m_despawnCoroutine);
        }

        m_explosionAudioSource.Play();
        m_hissAudioSource.Stop();
        
        m_fuseFireParticle.SetActive(false);
        GetComponent<Collider>().enabled = false;
        GetComponent<TrailRenderer>().enabled = false;
        foreach (MeshRenderer meshRenderer in GetComponentsInChildren<MeshRenderer>())
        {
            meshRenderer.enabled = false;
        }
        
        GameObject explosion = Instantiate(m_explosionPrefab, explosionCenter, Quaternion.identity);

        yield return new WaitForSeconds(m_explosionAliveSeconds);
        
        explosion.GetComponent<Collider>().enabled = false;
        yield return new WaitForSeconds(m_explosionAnimationSeconds - m_explosionAliveSeconds);
        
        Destroy(explosion);
        Destroy(this.gameObject);
    }
}
