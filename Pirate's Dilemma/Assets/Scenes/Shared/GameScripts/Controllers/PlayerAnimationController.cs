using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator m_animator;

    private CharacterController m_characterController;

    private bool m_initialized = false;
    
    private bool m_lastPosSet = false;

    private Vector3 m_lastPos;

    void Awake()
    {
        GameTimerSystem.Instance.m_onGameStart -= OnGameStart; //remove first just to be safe awake gets called each scene load
        GameTimerSystem.Instance.m_onGameStart += OnGameStart;
    }

    // Start is called before the first frame update
    public void OnGameStart()
    {
        m_animator = GetComponentInChildren<Animator>();
        m_characterController = GetComponent<CharacterController>();
        
        PlayerMovementController playerMovementController = GetComponent<PlayerMovementController>();

        playerMovementController.m_onPlayerStartDashCharge += OnStartDashCharge;
        playerMovementController.m_onPlayerStartDash += OnStartDash;
        playerMovementController.m_onPlayerDie += OnDie;

        PlayerGoldController playerGoldController = GetComponent<PlayerGoldController>();

        playerGoldController.m_onPlayerPickupGold += OnPickupGold;
        playerGoldController.m_onPlayerDropGold += OnDropGold;
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
    }

    void OnDropGold(int teamNum, int playerNum)
    {
        m_animator.SetTrigger("StartDrop");
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
