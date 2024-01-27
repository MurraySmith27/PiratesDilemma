using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput), typeof(PlayerDataController), typeof(GoldController))]
public class PlayerMovementController : MonoBehaviour
{
    [SerializeField] private float m_speed;
    
    private GoldController m_goldController;
    private PlayerInput m_playerInput;

    private InputAction m_moveAction;

    private bool m_intialized = false;

    // Update is called once per frame
    void Update()
    {
        if (m_intialized)
        {
            float speed = m_speed * ((100 - 2 * m_goldController.goldCarried) / 100f);
            Vector2 moveVector = m_moveAction.ReadValue<Vector2>().normalized * speed;
            transform.Translate(new Vector3(moveVector.x, 0f, moveVector.y));
        }
    }

    void OnGameStart()
    {
        m_goldController = GetComponent<GoldController>();
        m_playerInput = GetComponent<PlayerInput>();
        
        m_moveAction = m_playerInput.actions["Move"];

        m_intialized = true;
    }
    
    void Awake()
    {
        GameStartSystem.Instance.m_onGameStart += OnGameStart;

        m_intialized = false;
    }

}
