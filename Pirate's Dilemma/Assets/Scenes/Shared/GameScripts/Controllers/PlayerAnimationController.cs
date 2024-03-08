using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator m_animator;

    private CharacterController m_characterController;

    [SerializeField] private GameObject m_sweatParticle;

    private bool m_initialized = false;
    
    private bool m_lastPosSet = false;

    private Vector3 m_lastPos;

    void Awake()
    {
        GameTimerSystem.Instance.m_onGameStart -= OnGameStart; //remove first just to be safe awake gets called each scene load
        GameTimerSystem.Instance.m_onGameStart += OnGameStart;
        m_sweatParticle.SetActive(false);
    }

    // Start is called before the first frame update
    public void OnGameStart()
    {
        m_animator = GetComponentInChildren<Animator>();
        m_animator.Play("Idle", 0);
        m_characterController = GetComponent<CharacterController>();
        
        PlayerMovementController playerMovementController = GetComponent<PlayerMovementController>();

        playerMovementController.m_onPlayerStartDashCharge += OnStartDashCharge;
        playerMovementController.m_onPlayerStartDash += OnStartDash;
        playerMovementController.m_onPlayerDie += OnDie;
        playerMovementController.m_onDashCooldownStart += OnDashCooldownStart;
        

        PlayerGoldController playerGoldController = GetComponent<PlayerGoldController>();

        playerGoldController.m_onPlayerPickupGold += OnPickupGold;
        playerGoldController.m_onPlayerDropGoldOntoBoat += OnDropGold;
        playerGoldController.m_onPlayerStartThrowCharge += OnStartThrowCharge;
        playerGoldController.m_onPlayerStartThrow += OnStartThrow;

        m_initialized = true;
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

    void OnDashCooldownStart(int teamNum, int playerNum, float cooldownSeconds)
    {
        StartCoroutine(Sweat(cooldownSeconds));
    }

    private IEnumerator Sweat(float cooldownSeconds)
    {
        m_sweatParticle.SetActive(true);
        yield return new WaitForSeconds(cooldownSeconds);
        m_sweatParticle.SetActive(false);
    }
    
    void OnStartDashCharge()
    {
        m_animator.SetTrigger("StartDashCharge");
    }

    void OnStartDash()
    {
        m_animator.SetTrigger("StartDash");
    }

    void OnDie(int playerNum)
    {
        m_animator.SetTrigger("OnDeath");
    }
    
    void OnPickupGold(int teamNum, int playerNum)
    {
        m_animator.SetTrigger("StartPickup");
        m_animator.SetBool("CarryingGold", true);
    }

    void OnDropGold()
    {
        m_animator.SetTrigger("StartDrop");
        
        m_animator.SetBool("CarryingGold", false);
    }

    void OnStartThrowCharge()
    {
        m_animator.SetTrigger("StartThrowCharge");
    }

    void OnStartThrow()
    {
        m_animator.SetTrigger("StartThrow");
    }
}
