using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using FMODUnity;
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
    
    [SerializeField] private StudioEventEmitter m_hissEventEmitter;
    
    public LooseBombCollisionEvent m_onLooseBombCollision;

    public int m_lastHeldTeamNum;

    public int m_damagePerBomb = 1;

    private bool m_isLit = false;

    private Coroutine m_despawnCoroutine;

    private CinemachineImpulseSource m_cinemachineImpulseSource;
    
    void Start()
    {  
        m_despawnCoroutine = StartCoroutine(DespawnAfterSeconds());
        m_cinemachineImpulseSource = GetComponent<CinemachineImpulseSource>();
    }

    IEnumerator DespawnAfterSeconds()
    {
        yield return new WaitForSeconds(m_bombAliveSeconds);
        //Failsafe to stop hiss sound in case bomb doesn't explode on collision or get destroyed when it hits a killbox
        m_hissEventEmitter.Stop();
        Destroy(this.gameObject);
    }

    public void SetLit(bool isLit)
    {
        m_isLit = isLit;

        m_fuseFireParticle.SetActive(isLit);

        if (isLit)
        {
            m_hissEventEmitter.Play();
        }
        else
        {
            m_hissEventEmitter.Stop();

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
        else if (collider.gameObject.layer == LayerMask.NameToLayer("Killzone"))
        {
            //landed in water, bomb splashes in water and fizzles out if lit
            StartCoroutine(GenerateSplash());
               
   
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
        m_hissEventEmitter.Stop();
        
        m_fuseFireParticle.SetActive(false);
        GetComponent<Collider>().enabled = false;
        GetComponent<TrailRenderer>().enabled = false;
        foreach (MeshRenderer meshRenderer in GetComponentsInChildren<MeshRenderer>())
        {
            meshRenderer.enabled = false;
        }
        
        m_cinemachineImpulseSource.GenerateImpulse();
        
        GameObject explosion = Instantiate(m_explosionPrefab, explosionCenter, Quaternion.identity);

        yield return new WaitForSeconds(m_explosionAliveSeconds);
        
        explosion.GetComponent<Collider>().enabled = false;
        yield return new WaitForSeconds(m_explosionAnimationSeconds - m_explosionAliveSeconds);
        
        Destroy(explosion);
        Destroy(this.gameObject);
    }
    private IEnumerator GenerateSplash()
    {
        if (m_despawnCoroutine != null)
        {
            StopCoroutine(m_despawnCoroutine);
        }

        if (m_isLit)
        { 
            m_hissEventEmitter.Stop(); 
            m_fuseFireParticle.SetActive(false);  
        }
        
        
        //Add code here for Particle System to emmit water splash later and emmit water splash sound maybe
        
        GetComponent<Collider>().enabled = false;
        GetComponent<TrailRenderer>().enabled = false;
        
        yield return new WaitForSeconds(3);
        Destroy(this.gameObject);
    }
    
}
