using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(PlayerInput), typeof(PlayerData), typeof(PlayerGoldController))]
public class PlayerMovementController : MonoBehaviour
{
    public PlayerDieEvent m_onPlayerDie;
    
    [SerializeField] private float m_speed;
    
    // For dashing
    [SerializeField] private float m_dashDistance;
    [SerializeField] private float m_dashDuration;
    [SerializeField] private int m_numLeniencyFramesOnDashMovementInput = 6;
    
    // For when you get pushed
    [SerializeField] private float m_pushDistance;
    [SerializeField] private float m_pushDuration;
    
    [SerializeField] private bool m_isDashing;
    private bool m_isBeingPushed;
    
    private Coroutine m_dashCoroutine;
    private Coroutine m_beingPushedCoroutine;
    
    private PlayerGoldController m_playerGoldController;
    private PlayerInput m_playerInput;

    private InputAction m_dashAction;
    private InputAction m_moveAction;

    private PlayerData m_playerData;

    private bool m_initialized;

    public bool m_isThrowing;

    private float m_initialHeightFromFloor;
    
    private CharacterController m_characterController;
    
    private void Awake()
    {
        GameTimerSystem.Instance.m_onGameStart += OnGameStart;

        m_playerData = GetComponent<PlayerData>();

        m_characterController = GetComponent<CharacterController>();
        
        m_initialized = false;
    }
    
    private void FixedUpdate()
    {
        if (m_initialized && !m_isDashing && !m_isBeingPushed && !m_isThrowing)
        {
            float speed = m_speed * ((100 - 2 * m_playerGoldController.m_goldCarried) / 100f);
            Vector2 moveVector = -m_moveAction.ReadValue<Vector2>().normalized * (speed * Time.deltaTime);
            m_characterController.Move(new Vector3(moveVector.x, 0, moveVector.y));

            if (!m_characterController.isGrounded)
            {
                //cast ray to ground from bottom of capsule, move down by ray result

                float distanceToMoveDown = 9.81f * Time.deltaTime;
                RaycastHit hit;
                if (Physics.Raycast(
                        transform.position - new Vector3(0,
                            m_characterController.center.y + m_characterController.height / 2, 0), Vector3.down,
                        out hit, maxDistance: 0f, layerMask: ~LayerMask.GetMask("Floor")))
                {
                    distanceToMoveDown = hit.distance;
                }

                m_characterController.Move(new Vector3(0, -distanceToMoveDown, 0));
            }
        }
    }

    public void OnGameStart()
    {
        m_playerGoldController = GetComponent<PlayerGoldController>();
        m_playerInput = GetComponent<PlayerInput>();
        
        m_moveAction = m_playerInput.actions["Move"];
        m_dashAction = m_playerInput.actions["Dash"];
        
        m_isDashing = false;
        m_isBeingPushed = false;

        m_dashAction.performed += OnDash;

        m_initialized = true;
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position, new Vector3(0, -1, 0), maxDistance: 0f, hitInfo: out hit,
                layerMask: ~LayerMask.GetMask(new string[]{"Floor"})))
        {
            m_initialHeightFromFloor = hit.distance;
        }
    }

    public void OnGameStop()
    {
        m_initialized = false;

        m_dashAction.performed -= OnDash;
    }

    private void OnDash(InputAction.CallbackContext ctx)
    {
        if (m_initialized && !m_isDashing && !m_isThrowing && m_playerGoldController.m_goldCarried == 0)
        {
            Vector2 movementInput = -m_moveAction.ReadValue<Vector2>();
            m_dashCoroutine = StartCoroutine(DashCoroutine(movementInput));
        }
    }

    private IEnumerator DashCoroutine(Vector2 dashDirection)
    {
        m_isDashing = true;
        int numLeniencyFramesCounted = 0;
        while (dashDirection.magnitude == 0)
        {
            yield return null;
            numLeniencyFramesCounted++;
            if (numLeniencyFramesCounted > m_numLeniencyFramesOnDashMovementInput)
            {
                m_isDashing = false;
                yield break;
            }
            dashDirection = -m_moveAction.ReadValue<Vector2>();
        }

        Vector3 initial = transform.position;
        Vector2 dashVector = dashDirection.normalized * m_dashDistance;
        Vector3 final = new Vector3(initial.x + dashVector.x, initial.y, initial.z + dashVector.y);
        
        float finalDistance = m_dashDistance;
        
        //do a raycast, see if we need to stop early because we're hitting a wall.
        RaycastHit hit;
        if (Physics.Raycast(initial, (final - initial).normalized, out hit, layerMask: LayerMask.GetMask(new string[]{"StaticObstacle"}), maxDistance: m_dashDistance))
        {
            finalDistance = hit.distance;
        }
        
        Vector3 pos;
        for (float t = 0; t < 1; t += Time.deltaTime / m_dashDuration)
        {
            yield return new WaitForFixedUpdate();

            float progress = Mathf.Pow(t, 1f / 3f);
            pos = initial + (final - initial) * progress - transform.position;
            m_characterController.Move(new Vector3(pos.x, 0, pos.z));

            if (progress * m_dashDistance >= finalDistance)
            {
                break;
            }
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
    }

    private void OnTriggerStay(Collider otherCollider)
    {
        if (otherCollider.gameObject.layer == LayerMask.NameToLayer("Killzone") && !m_isDashing)
        {
            //player dies.
            m_onPlayerDie(m_playerData.m_playerNum);
        }
    }

    public void GetPushed(Vector2 dashDirection)
    {
        if (m_playerGoldController.m_goldCarried > 0)
        {
            m_playerGoldController.SpawnLooseGold(false);
        }
        m_playerGoldController.DropAllGold();
        
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
        
        float finalDistance = m_pushDistance;
        
        //do a raycast, see if we need to stop early because we're hitting a wall.
        RaycastHit hit;
        if (Physics.Raycast(initial, (final - initial).normalized, out hit, layerMask: LayerMask.GetMask(new string[]{"StaticObstacle"}), maxDistance: m_pushDistance))
        {
            finalDistance = hit.distance;
        }
        
        Vector3 pos;
        for (float t = 0; t < 1; t += Time.deltaTime / m_pushDuration)
        {
            yield return new WaitForFixedUpdate();

            float progress = Mathf.Pow(t, 1f / 3f);
            pos = initial + (final - initial) * progress;
            m_characterController.Move(pos - transform.position);

            if (progress * m_pushDistance >= finalDistance)
            {
                break;
            }
        }

        m_isBeingPushed = false;
    }

    public void WarpToPosition(Vector3 position)
    {
        m_characterController.enabled = false;
        transform.position = position;
        m_characterController.enabled = true;
    }


    void OnEnable()
    {
        if (m_dashAction != null)
        {
            m_dashAction.Enable();
        }

        if (m_moveAction != null)
        {
            m_moveAction.Enable();
        }
}

    void OnDisable()
    {
        if (m_dashAction != null)
        {
            m_dashAction.Disable();
        }

        if (m_moveAction != null)
        {
            m_moveAction.Disable();
        }
    }
}
