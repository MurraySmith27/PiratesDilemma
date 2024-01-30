using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput), typeof(PlayerData), typeof(PlayerGoldController))]
public class PlayerMovementController : MonoBehaviour
{
    [SerializeField] private float m_speed;
    
    private PlayerGoldController m_PlayerGoldController;
    private PlayerInput m_playerInput;

    private InputAction m_moveAction;

    private bool m_intialized = false;

    [SerializeField] private Rigidbody rb;
    
    // Update is called once per frame
    void FixedUpdate()
    {
        if (m_intialized)
        {
            float speed = m_speed * ((100 - 2 * m_PlayerGoldController.m_goldCarried) / 100f);
            Vector2 moveVector = - m_moveAction.ReadValue<Vector2>().normalized * (speed * Time.deltaTime);
            rb.MovePosition(transform.position + new Vector3(moveVector.x, 0f, moveVector.y));
        }
    }

    void OnGameStart()
    {
        m_PlayerGoldController = GetComponent<PlayerGoldController>();
        m_playerInput = GetComponent<PlayerInput>();
        
        m_moveAction = m_playerInput.actions["Move"];

        m_intialized = true;
    }
    
    void Awake()
    {
        GameTimerSystem.Instance.m_onGameStart += OnGameStart;
        
        m_intialized = false;
    }

}
