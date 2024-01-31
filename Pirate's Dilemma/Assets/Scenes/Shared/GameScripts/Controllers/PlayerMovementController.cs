using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public delegate void OnPlayerDie(int playerNum);

[RequireComponent(typeof(PlayerInput), typeof(PlayerData), typeof(PlayerGoldController))]
public class PlayerMovementController : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    
    [SerializeField] private float m_speed;
    
    // For dashing
    [SerializeField] private float m_dashDistance;
    [SerializeField] private float m_dashDuration;
    
    // For when you get pushed
    [SerializeField] private float m_pushDistance;
    [SerializeField] private float m_pushDuration;
    
    private bool m_isDashing;
    private bool m_isBeingPushed;
    
    private Coroutine m_dashCoroutine;
    private Coroutine m_beingPushedCoroutine;
    
    private PlayerGoldController m_PlayerGoldController;
    private PlayerInput m_playerInput;

    private InputAction m_dashAction;
    private InputAction m_moveAction;

    private bool m_intialized;


    
    private void Awake()
    {
        GameTimerSystem.Instance.m_onGameStart += OnGameStart;
        
        m_intialized = false;
    }
    
    private void FixedUpdate()
    {
        if (m_intialized && !m_isDashing && !m_isBeingPushed)
        {
            float speed = m_speed * ((100 - 2 * m_PlayerGoldController.m_goldCarried) / 100f);
            Vector2 moveVector = -m_moveAction.ReadValue<Vector2>().normalized * (speed * Time.deltaTime);
            rb.MovePosition(transform.position + new Vector3(moveVector.x, 0f, moveVector.y));
        }
    }

    private void OnGameStart()
    {
        m_PlayerGoldController = GetComponent<PlayerGoldController>();
        m_playerInput = GetComponent<PlayerInput>();
        
        m_moveAction = m_playerInput.actions["Move"];
        m_dashAction = m_playerInput.actions["Dash"];
        
        m_isDashing = false;
        m_isBeingPushed = false;

        m_dashAction.performed += OnDash;

        m_intialized = true;
    }

    private void OnDash(InputAction.CallbackContext ctx)
    {
        if (!m_isDashing)
        {
            m_isDashing = true;
            Vector2 movementInput = -m_moveAction.ReadValue<Vector2>();
            m_dashCoroutine = StartCoroutine(DashCoroutine(movementInput));
        }
    }

    private IEnumerator DashCoroutine(Vector2 dashDirection)
    {
        Vector3 initial = transform.position;
        Vector2 dashVector = -m_moveAction.ReadValue<Vector2>().normalized * m_dashDistance;
        Vector3 final = new Vector3(initial.x + dashVector.x, initial.y, initial.z + dashVector.y);
        Vector3 pos;
        for (float t = 0; t < 1; t += Time.deltaTime / m_dashDuration)
        {
            yield return new WaitForFixedUpdate();
            pos = initial + (final - initial) * Mathf.Pow(t, 1f / 3f);
            rb.MovePosition(pos);
        }

        m_isDashing = false;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player") && m_isDashing)
        {
            PlayerMovementController otherPlayerMovement = collision.gameObject.GetComponent<PlayerMovementController>();

            if (otherPlayerMovement != null)
            {
                Vector3 direction = collision.transform.position - transform.position;
                direction.y = 0;
                Vector2 dashDirection = new Vector2(direction.x, direction.z).normalized;

                otherPlayerMovement.GetPushed(dashDirection);
            }
        }   
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Killzone"))
        {
            //player dies.
            
        }
    }

    public void GetPushed(Vector2 dashDirection)
    {
        if (m_isDashing)
        {
            //if attacked while dashing, stop dashing and get pushed.
            StopCoroutine(m_dashCoroutine);
            m_isDashing = false;
        }

        if (m_isBeingPushed)
        {
            StopCoroutine(m_beingPushedCoroutine);
        }
        m_beingPushedCoroutine = StartCoroutine(GetPushedCoroutine(dashDirection));
        m_isBeingPushed = true;
    }

    private IEnumerator GetPushedCoroutine(Vector2 dashDirection)
    {
        Vector3 initial = transform.position;
        Vector3 final = initial + new Vector3(dashDirection.x, 0, dashDirection.y) * m_pushDistance;
        Vector3 pos;
        for (float t = 0; t < 1; t += Time.deltaTime / m_pushDuration)
        {
            yield return new WaitForFixedUpdate();
            pos = initial + (final - initial) * Mathf.Pow(t, 1f / 3f);
            rb.MovePosition(pos);
        }

        m_isBeingPushed = false;
    }

}
