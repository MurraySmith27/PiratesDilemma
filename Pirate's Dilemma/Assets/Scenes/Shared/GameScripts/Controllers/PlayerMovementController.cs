using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public delegate void PlayerGetPushedEvent();

[RequireComponent(typeof(PlayerInput), typeof(PlayerData), typeof(PlayerGoldController))]
public class PlayerMovementController : MonoBehaviour
{
    public PlayerDieEvent m_onPlayerDie;

    public PlayerGetPushedEvent m_onPlayerGetPushed;
    
    [SerializeField] private float m_speed;

    [SerializeField] private float m_onCollideWithGoldForce = 1f;
    
    // For dashing
    [SerializeField] private GameObject m_dashTargetGameObject;
    [SerializeField] private float m_timeToChargeToMaxDashRange;
    [SerializeField] private float m_minDashDistance;
    [SerializeField] private float m_maxDashDistance;
    [SerializeField] private float m_dashDuration;
    [SerializeField] private float m_chargeDashMoveSpeed;
    [SerializeField] private GameObject m_dashIndicatorArrowBodyGameObject;
    
    // For when you get pushed
    [SerializeField] private float m_pushDistance;
    [SerializeField] private float m_pushDuration;
    
    public Transform m_feetPosition;
    
    
    private bool m_isChargingDash;
    
    private bool m_isDashing;
    

    private bool m_isBeingPushed;
    
    private Coroutine m_dashCoroutine;
    private Coroutine m_beingPushedCoroutine;
    
    private PlayerGoldController m_playerGoldController;
    private PlayerInput m_playerInput;

    private InputAction m_dashAction;
    private InputAction m_moveAction;

    private PlayerData m_playerData;

    private bool m_initialized;
    
    private CharacterController m_characterController;

    private Coroutine m_dashChargeUpCoroutine;

    
    private void Awake()
    {
        GameTimerSystem.Instance.m_onGameStart += OnGameStart;
        GameTimerSystem.Instance.m_onGameFinish += OnGameStop;

        m_playerData = GetComponent<PlayerData>();

        m_characterController = GetComponent<CharacterController>();
        
        m_initialized = false;
    }
    
    private void FixedUpdate()
    {
        Vector2 moveInput = -m_moveAction.ReadValue<Vector2>().normalized;
        if (m_initialized && !m_isDashing && !m_isBeingPushed && !m_playerGoldController.IsOccupied())
        {
            float speed = m_speed;
            if (m_isChargingDash)
            {
                speed = m_chargeDashMoveSpeed;
            }
            
            Vector2 moveVector = moveInput * (speed * Time.deltaTime);
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

        if (moveInput.magnitude != 0 && !m_isDashing && !m_isBeingPushed)
        {
            transform.LookAt(transform.position + new Vector3(moveInput.x, 0, moveInput.y));
        }
    }

    public void OnGameStart()
    {
        m_playerGoldController = GetComponent<PlayerGoldController>();
        m_playerInput = GetComponent<PlayerInput>();
        
        m_moveAction = m_playerInput.actions["Move"];
        m_dashAction = m_playerInput.actions["Dash"];
        
        m_isChargingDash = false;
        m_isDashing = false;
        m_isBeingPushed = false;

        m_dashAction.performed += OnDashButtonHeld;
        m_dashAction.canceled += OnDashButtonReleased;

        m_initialized = true;
    }

    public void OnGameStop()
    {
        m_initialized = false;

        m_dashAction.performed -= OnDashButtonHeld;
        m_dashAction.canceled -= OnDashButtonReleased;
    }

    public bool IsOccupied()
    {
        return m_isDashing || m_isBeingPushed || m_isChargingDash;
    }
    
    private void OnDashButtonHeld(InputAction.CallbackContext ctx)
    {
        if (m_initialized && !IsOccupied() && !m_playerGoldController.IsOccupied() && m_playerGoldController.m_goldCarried == 0)
        {
            m_isChargingDash = true;
            m_dashChargeUpCoroutine = StartCoroutine(DashChargeUpCoroutine());
        }
    }

    private IEnumerator DashChargeUpCoroutine()
    {
        m_dashTargetGameObject.SetActive(true);
        m_dashTargetGameObject.transform.position = m_feetPosition.position;

        m_dashIndicatorArrowBodyGameObject.SetActive(true);

        float t = 0;
        while (true)
        {
        
            Vector3 initialPos = m_feetPosition.position;
            Vector3 maxDistancePos = initialPos + transform.forward * m_maxDashDistance;
            t = Mathf.Min(t + Time.deltaTime / m_timeToChargeToMaxDashRange, 1);
            
            Vector3 targetPos = initialPos + (maxDistancePos - initialPos) * t;
            
            //need to check if the dash target is in a wall. if so truncate the dash.
            RaycastHit hit;
            if (Physics.Raycast(initialPos, (targetPos - initialPos).normalized, out hit, layerMask: LayerMask.GetMask(new string[]{"StaticObstacle"}), maxDistance: (targetPos - initialPos).magnitude))
            {
                targetPos = initialPos + (maxDistancePos - initialPos) * (hit.distance / m_maxDashDistance);
            }
            
            m_dashTargetGameObject.transform.position = targetPos;

            m_dashIndicatorArrowBodyGameObject.transform.localScale = new Vector3(
                m_dashIndicatorArrowBodyGameObject.transform.localScale.x, 
                m_dashTargetGameObject.transform.localPosition.z - 0.25f, 
                m_dashIndicatorArrowBodyGameObject.transform.localScale.z
            );
            m_dashIndicatorArrowBodyGameObject.transform.localPosition =
                new Vector3(0, 0, (m_dashTargetGameObject.transform.localPosition.z / 2f));
            
            
            yield return null;
        }    
    }

    private void OnDashButtonReleased(InputAction.CallbackContext ctx)
    {
        if (m_isChargingDash && m_dashChargeUpCoroutine != null)
        {
            StopCoroutine(m_dashChargeUpCoroutine);
            
            m_dashTargetGameObject.SetActive(false);
            m_dashIndicatorArrowBodyGameObject.SetActive(false);
            
            Vector3 endPosition = m_dashTargetGameObject.transform.position;
            if ((endPosition - m_feetPosition.position).magnitude > m_minDashDistance)
            {
                m_dashCoroutine = StartCoroutine(DashCoroutine(endPosition));
            }
            else
            {
                m_isDashing = false;
            }
            m_isChargingDash = false;
        }
    }
    
    private IEnumerator DashCoroutine(Vector3 endPos)
    {
        m_isDashing = true;
        Vector3 initialPos = transform.position;
        
        Vector3 pos;
        for (float t = 0; t < 1; t += Time.deltaTime / m_dashDuration)
        {
            yield return new WaitForFixedUpdate();

            float progress = Mathf.Pow(t, 1f / 3f);
            pos = initialPos + (endPos - initialPos) * progress - transform.position;
            m_characterController.Move(new Vector3(pos.x, 0, pos.z));
        }

        m_isDashing = false;
    }
    

    private void OnTriggerStay(Collider otherCollider)
    {
        if (otherCollider.gameObject.layer == LayerMask.NameToLayer("Killzone") && !m_isDashing)
        {
            //player dies.
            m_onPlayerDie(m_playerData.m_playerNum);
        }
    }


    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.layer == LayerMask.NameToLayer("LooseGold"))
        {
            hit.rigidbody.AddForceAtPosition((hit.gameObject.transform.position - transform.position).normalized *
                                   m_onCollideWithGoldForce * m_characterController.velocity.magnitude, transform.position, ForceMode.Impulse);
        }
        else if (hit.gameObject.layer == LayerMask.NameToLayer("Player") && m_isDashing)
        {
            PlayerMovementController otherPlayerMovement = hit.gameObject.GetComponent<PlayerMovementController>();

            if (otherPlayerMovement != null)
            {
                Vector3 direction = hit.transform.position - transform.position;
                direction.y = 0;
                Vector2 dashDirection = new Vector2(direction.x, direction.z).normalized;
                otherPlayerMovement.GetPushed(dashDirection);
            }
        } 
    }

    public void GetPushed(Vector2 dashDirection)
    {
        if (m_isChargingDash)
        {
            //if attacked while charging a dash, stop charging and get pushed.
            StopCoroutine(m_dashChargeUpCoroutine);
            m_isChargingDash = false;
        }
        
        if (m_isDashing)
        {
            //if attacked while dashing, stop dashing and get pushed.
            StopCoroutine(m_dashCoroutine);
            m_isDashing = false;
        }

        if (!m_isBeingPushed)
        {
            m_beingPushedCoroutine = StartCoroutine(GetPushedCoroutine(dashDirection));
            m_isBeingPushed = true;

            m_onPlayerGetPushed();
        }
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
