using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using FMODUnity;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator m_animator;

    private CinemachineImpulseSource m_cinemachineImpulseSource;
    
    private CharacterController m_characterController;

    [SerializeField] private GameObject m_sweatParticle;

    [SerializeField] private float m_damageFlashFalloffTime = 0.3f;
    
    [SerializeField] private GameObject m_onHitParticlePrefab;

    [SerializeField] private float m_onHitParticleDuration = 1f;

    [SerializeField] private float m_onHitParticleSize = 3f;
    
    [SerializeField] private StudioEventEmitter m_splashEventEmitter;

    [SerializeField, ColorUsage(true, true)] private Color m_damageFlashColor = Color.white;
    
    [SerializeField] private GameObject m_playerModel;

    [SerializeField] private GameObject m_playerTrailParticleSystem; 
        
    private bool m_initialized = false;
    
    private bool m_lastPosSet = false;

    private bool m_alive = true;

    private Vector3 m_lastPos;

    private PlayerData m_playerData;
    
    

    void Awake()
    {
        GameTimerSystem.Instance.m_onGameStart -= OnGameStart; //remove first just to be safe awake gets called each scene load
        GameTimerSystem.Instance.m_onGameStart += OnGameStart;
        m_sweatParticle.SetActive(false);
    }

    private void Start()
    {
        m_cinemachineImpulseSource = GetComponent<CinemachineImpulseSource>();

        m_playerData = GetComponent<PlayerData>();
        
        m_playerTrailParticleSystem.SetActive(true);
        
    }
    
    public void OnGameStart()
    {
        m_animator = GetComponentInChildren<Animator>();
        m_animator.Play("Idle", 0);
        m_characterController = GetComponent<CharacterController>();
        m_playerTrailParticleSystem.SetActive(true);
        
        PlayerMovementController playerMovementController = GetComponent<PlayerMovementController>();

        playerMovementController.m_onPlayerStartDashCharge += OnStartDashCharge;
        playerMovementController.m_onPlayerStartDash += OnStartDash;
        playerMovementController.m_onPlayerDie += OnDie;
        playerMovementController.m_onDashCooldownStart += OnDashCooldownStart;
        playerMovementController.m_onPlayerGetPushed += OnGetPushed;
        
        

        PlayerItemController playerItemController = GetComponent<PlayerItemController>();

        playerItemController.m_onPlayerPickupBomb += OnPickupBomb;
        playerItemController.m_onPlayerStartThrowCharge += OnStartThrowCharge;
        playerItemController.m_onPlayerStartThrow += OnStartThrow;

        m_initialized = true;

        m_alive = true;
    }
    
    public void SetInvulnerableMaterial(float invulnerableTime = -1)
    {
        if (invulnerableTime == -1)
        {
            invulnerableTime = GetComponent<PlayerMovementController>().m_invulnerableTimeOnRespawn;
        }

        StartCoroutine(SetInvulnerableMaterialCoroutine(invulnerableTime));
    }

    private IEnumerator SetInvulnerableMaterialCoroutine(float invulnerableTime)
    {
        Renderer[] meshRenderers = m_playerModel.GetComponentsInChildren<Renderer>();
        
        List<List<Material>> materialsPerChild = new List<List<Material>>();

        //add transparent material to all child meshes.
        foreach (Renderer meshRenderer in meshRenderers)
        {
            List<Material> materials = new List<Material>();
            meshRenderer.GetMaterials(materials);
            materials[1].SetFloat("_IsActive", 1f);
            materialsPerChild.Add(materials);
            // List<Material> materialsCopy = new List<Material>(materials);
            // materialsCopy.Add(m_invulnerableMaterial);
            // meshRenderer.SetMaterials(materialsCopy);
        }

        
        yield return new WaitForSeconds(invulnerableTime);
        
        
        //reset all materials
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            // meshRenderers[i].SetMaterials(materialsPerChild[i]);
            materialsPerChild[i][1].SetFloat("_IsActive", 0f);
        }
    }

    void FixedUpdate()
    {
        if (m_initialized)
        {
            if (!m_lastPosSet)
            {
                m_lastPosSet = true;
                m_lastPos = transform.position;
                return;
            }

            Vector3 diff = m_lastPos - transform.position;
            float currentVelocity =
                new Vector2(diff.x, diff.z).magnitude / Time.deltaTime;
            
            m_animator.SetFloat("MoveSpeed", currentVelocity);
            
            m_lastPos = transform.position;
        }
        
    }

    public void OnRespawn()
    {
        m_alive = true;
        m_animator.SetTrigger("Respawn");
        m_playerTrailParticleSystem.SetActive(true);
    }

    void OnDashCooldownStart(int teamNum, int playerNum, float cooldownSeconds)
    {
        StartCoroutine(DashCooldownAnims(cooldownSeconds));
    }

    private IEnumerator DashCooldownAnims(float cooldownSeconds)
    {
        m_sweatParticle.SetActive(true);
        
        Renderer[] meshRenderers = m_playerModel.GetComponentsInChildren<Renderer>();
        
        float t = 0;
        while (t < cooldownSeconds)
        {
            foreach (Renderer meshRenderer in meshRenderers)
            {
                List<Material> materials = new List<Material>();
                meshRenderer.GetMaterials(materials);
                materials[3].SetFloat("_CooldownProgress", t / cooldownSeconds);
            }
            
            yield return null;
            t += Time.deltaTime;
        }
        foreach (Renderer meshRenderer in meshRenderers)
        {
            List<Material> materials = new List<Material>();
            meshRenderer.GetMaterials(materials);
            materials[3].SetFloat("_CooldownProgress",1f);
        }
        m_sweatParticle.SetActive(false);
    }
    
    void OnStartDashCharge()
    {
        m_animator.SetTrigger("StartDashCharge");
    }

    void OnStartDash(float dashDurationSeconds)
    {
        m_animator.SetTrigger("StartDash");
        StartCoroutine(DashShaderAnim(dashDurationSeconds));
    }

    private IEnumerator DashShaderAnim(float dashDurationSeconds)
    {
        Renderer[] meshRenderers = m_playerModel.GetComponentsInChildren<Renderer>();
        
        float t = 0;
        while (t < dashDurationSeconds)
        {
            foreach (Renderer meshRenderer in meshRenderers)
            {
                List<Material> materials = new List<Material>();
                meshRenderer.GetMaterials(materials);
                materials[3].SetFloat("_CooldownProgress", 1f - (t / dashDurationSeconds));
            }
            
            yield return null;
            t += Time.deltaTime;
        }
        foreach (Renderer meshRenderer in meshRenderers)
        {
            List<Material> materials = new List<Material>();
            meshRenderer.GetMaterials(materials);
            materials[3].SetFloat("_CooldownProgress",0f);
        }
        
    }

    void OnDie(int playerNum)
    {
        if (m_alive)
        {
            m_splashEventEmitter.Play();
            m_animator.SetTrigger("OnDeath");
            m_animator.SetBool("CarryingBomb", false);
            m_playerTrailParticleSystem.SetActive(false);
            m_alive = false;
        }
    }
    
    void OnPickupBomb(int teamNum, int playerNum)
    {
        m_animator.SetTrigger("StartPickup");
        m_animator.SetBool("CarryingBomb", true);
    }
    
    void OnStartThrowCharge()
    {
        m_animator.SetTrigger("StartThrowCharge");
    }

    void OnStartThrow()
    {
        m_animator.SetTrigger("StartThrow");
        m_animator.SetBool("CarryingBomb", false);
    }

    void OnGetPushed(Vector3 contactPosition)
    {
        m_animator.SetBool("CarryingBomb", false);
        
        //reset dash cooldown effect
        Renderer[] meshRenderers = m_playerModel.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer meshRenderer in meshRenderers)
        {
            List<Material> materials = new List<Material>();
            meshRenderer.GetMaterials(materials);
            materials[3].SetFloat("_CooldownProgress",1f);
        }
        
        //generate screen shake impulse
        m_cinemachineImpulseSource.GenerateImpulse();

        GameObject hitParticle = Instantiate(m_onHitParticlePrefab, contactPosition, Quaternion.LookRotation(contactPosition - transform.position));

        foreach (ParticleSystem system in hitParticle.GetComponentsInChildren<ParticleSystem>())
        {
            system.transform.localScale *= m_onHitParticleSize;
        }
        
        Destroy(hitParticle, m_onHitParticleDuration);
            
        StartCoroutine(CreateDamageFlash());
    }

    private IEnumerator CreateDamageFlash()
    {
        Renderer[] meshRenderers = m_playerModel.GetComponentsInChildren<Renderer>();

        List<List<Material>> materialsPerChild = new List<List<Material>>();

        //add transparent material to all child meshes.
        foreach (Renderer meshRenderer in meshRenderers)
        {
            List<Material> materials = new List<Material>();
            meshRenderer.GetMaterials(materials);
            materials[2].SetColor("_FlashColor", m_damageFlashColor);
            materialsPerChild.Add(materials);
        }


        float currentTime = 0f;
        while (currentTime < m_damageFlashFalloffTime)
        {
            currentTime += Time.deltaTime;

            float currentFlash = 1 - (currentTime / m_damageFlashFalloffTime);
            foreach (List<Material> childMaterials in materialsPerChild)
            {
                childMaterials[2].SetFloat("_FlashAmount", currentFlash);
            }

            yield return null;
        }

        foreach (List<Material> childMaterials in materialsPerChild)
        {
            childMaterials[2].SetFloat("_FlashAmount", 0f);
        }
    }
}
