using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using FMODUnity;
using UnityEngine;

public delegate void LooseBombCollisionEvent();

public delegate void BombExplosionEvent();
public class BombController : MonoBehaviour
{
    [SerializeField] private float m_bombAliveSeconds = 5f;

    [SerializeField] private float m_minBombFlashSpeed = 1f;
    
    [SerializeField] private float m_maxBombFlashSpeed = 10f;

    [SerializeField] private AnimationCurve m_bombFlashSpeedOverTimeAnimCurve;
    
    [SerializeField] private GameObject m_explosionPrefab;
    
    [SerializeField] private float m_explosionAliveSeconds;

    [SerializeField] private float m_explosionAnimationSeconds = 2f;

    [SerializeField] private GameObject m_fuseFireParticle;

    [SerializeField] private bool m_isLoose = false;
    
    [SerializeField] private StudioEventEmitter m_hissEventEmitter;

    [SerializeField] private StudioEventEmitter m_splashEventEmitter;

    [SerializeField] private Animator m_bombAnimator;

    public BombExplosionEvent m_onBombExplode;
    
    public LooseBombCollisionEvent m_onLooseBombCollision;

    [HideInInspector] public int m_lastHeldTeamNum = -1;

    public int m_damagePerBomb = 1;

    public bool m_isLit { get; private set; }

    [HideInInspector] public bool m_wasThrown = false;

    private Coroutine m_despawnCoroutine;

    private CinemachineImpulseSource m_cinemachineImpulseSource;
    
    void Start()
    {  
        m_cinemachineImpulseSource = GetComponent<CinemachineImpulseSource>();
    }
    

    IEnumerator BlowUpAfterSeconds()
    {
        m_bombAnimator.SetTrigger("StartLit");
        
        foreach (Renderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            //material at index 1 is the
            List<Material> mats = new List<Material>();
            renderer.GetMaterials(mats);
            mats[1].SetFloat("_IsActive", 1f);
            renderer.SetMaterials(mats);
        }
        
        float t = 0;
        while (t <= m_bombAliveSeconds)
        {
            float progress = m_bombFlashSpeedOverTimeAnimCurve.Evaluate(t / m_bombAliveSeconds);
            
            float currentFlashSpeed = m_minBombFlashSpeed * (1f - progress) + m_maxBombFlashSpeed * progress;

            foreach (Renderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                //material at index 1 is the
                List<Material> mats = new List<Material>();
                renderer.GetMaterials(mats);
                mats[1].SetFloat("_FlashSpeed", currentFlashSpeed);
                renderer.SetMaterials(mats);
            }
            
            
            if (t > 0.9f * m_bombAliveSeconds)
            {
                //start stretching animation
                m_bombAnimator.SetTrigger("StartPreExplosion");
            }
            yield return null;
            t += Time.deltaTime;
        }

        GenerateExplosion(transform.position);
        Destroy(this.gameObject);
    }

    private void OnDestroy()
    {
        m_hissEventEmitter.Stop();
    }

    public void SetLit(bool isLit)
    {
        m_isLit = isLit;

        m_fuseFireParticle.SetActive(isLit);

        if (isLit)
        {
            m_despawnCoroutine = StartCoroutine(BlowUpAfterSeconds());
            m_hissEventEmitter.Play();
        }
        else
        {
            m_hissEventEmitter.Stop();

        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        
        if (collider.gameObject.layer == LayerMask.NameToLayer("Killzone"))
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
        
        if (collision.gameObject.layer == LayerMask.NameToLayer("Boat") && m_isLit && m_wasThrown)
        {   
            GenerateExplosion(collision.contacts[0].point);
        }
    }

    private void GenerateExplosion(Vector3 explosionCenter)
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

        ExplosionController explosionController = explosion.GetComponent<ExplosionController>();

        explosionController.m_explosionAliveSeconds = m_explosionAliveSeconds;
        explosionController.m_explosionAnimationSeconds = m_explosionAnimationSeconds;
        explosionController.m_teamNum = m_lastHeldTeamNum;
        explosionController.m_boatDamage = m_damagePerBomb;

        if (m_onBombExplode != null && m_onBombExplode.GetInvocationList().Length > 0)
        {
            m_onBombExplode();
        }
        
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

        m_splashEventEmitter.Play();
        //Add code here for Particle System to emmit water splash later and emmit water splash sound maybe
        
        GetComponent<Collider>().enabled = false;
        GetComponent<TrailRenderer>().enabled = false;
        
        yield return new WaitForSeconds(3);
        Destroy(this.gameObject);
    }
    
}
