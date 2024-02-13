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
    
    // For dashing
    [SerializeField] private GameObject m_dashTargetGameObject;
    [SerializeField] private float m_timeToChargeToMaxDashRange;
    [SerializeField] private float m_minDashDistance;
    [SerializeField] private float m_maxDashDistance;
    [SerializeField] private float m_dashDuration;
    [SerializeField] private float m_chargeDashMoveSpeed;
    [SerializeField] private Transform m_dashIndicatorArrowHeadTransform;
    [SerializeField] private Transform m_dashIndicatorArrowBodyTransform;
    
    // For when you get pushed
    [SerializeField] private float m_pushDistance;
    [SerializeField] private float m_pushDuration;
    
    [SerializeField] private Transform m_feetPosition;
    
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

    private float m_initialHeightFromFloor;
    
    private CharacterController m_characterController;

    private Coroutine m_dashChargeUpCoroutine;

    
    private void Awake()
    {
        GameTimerSystem.Instance.m_onGameStart += OnGameStart;

        m_playerData = GetComponent<PlayerData>();

        m_characterController = GetComponent<CharacterController>();
        
        m_initialized = false;
    }
    
    private void FixedUpdate()
    {
        if (m_initialized && !m_isDashing && !m_isBeingPushed && !m_playerGoldController.IsOccupied())
        {
            float speed = m_speed;
            if (m_isChargingDash)
            {
                speed = m_chargeDashMoveSpeed;
            }
            
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
        
        m_isChargingDash = false;
        m_isDashing = false;
        m_isBeingPushed = false;

        m_dashAction.performed += OnDashButtonHeld;
        m_dashAction.canceled += OnDashButtonReleased;

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


        Vector2 moveVector = m_moveAction.ReadValue<Vector2>();
        if (moveVector.magnitude <= 0.01f)
        {
            moveVector = new Vector2(1f, 0f);
        }
        // float heightDeltaWithFloor = transform.position.y;
        // RaycastHit hit;
        // if (Physics.Raycast(transform.position, new Vector3(0, -1, 0), maxDistance: 0f, hitInfo: out hit,
        //         layerMask: ~LayerMask.GetMask(new string[]{"Floor"})))
        // {
        //     heightDeltaWithFloor = hit.distance;
        // }
        
        
        // LineRenderer trajectoryLine = GetComponent<LineRenderer>();

        float t = 0;
        while (true)
        {
            
            Vector3 initialPos = m_feetPosition.position;
            Vector3 maxDistancePos = initialPos + new Vector3(moveVector.x, 0, moveVector.y) * m_maxDashDistance;

            t = Mathf.Min(t + Time.deltaTime * m_timeToChargeToMaxDashRange, 1);
            maxDistancePos = initialPos + new Vector3(moveVector.x, 0, moveVector.y) * m_maxDashDistance;
            
            Vector2 newMoveVector = -m_moveAction.ReadValue<Vector2>();
            if (newMoveVector.magnitude != 0)
            {
                moveVector = newMoveVector;
            }
            // trajectoryLine.positionCount = m_trajectoryLineResolution;

            Vector3 targetPos = initialPos + (maxDistancePos - initialPos) * t;
            
            //need to check if the dash target is in a wall. if so truncate the dash.
            RaycastHit hit;
            if (Physics.Raycast(initialPos, (targetPos - initialPos).normalized, out hit, layerMask: LayerMask.GetMask(new string[]{"StaticObstacle"}), maxDistance: (targetPos - initialPos).magnitude))
            {
                targetPos = initialPos + (maxDistancePos - initialPos) * (hit.distance / m_maxDashDistance);
            }
            
            m_dashTargetGameObject.transform.position = targetPos;
            

            // float currentDistance = ((maxDistancePos - initialPos) * t).magnitude;
            
            // List<Vector3> linePositions = new List<Vector3>();
            // for (int i = 0; i < m_trajectoryLineResolution; i++)
            // {
            //     //progress along line
            //     float t2 = i / (float)m_trajectoryLineResolution;
            //     float currentHeight = -(t2 * currentDistance - currentDistance) * (t2 * currentDistance + (heightDeltaWithFloor / currentDistance));
            //     Vector3 currentPositionAlongLine = initialPos + (targetPos - initialPos) * t2;
            //     linePositions.Add(new Vector3(currentPositionAlongLine.x, currentHeight, currentPositionAlongLine.z));
            // }
            //
            // trajectoryLine.SetPositions(linePositions.ToArray());
            
            yield return null;
        }    
    }

    private void OnDashButtonReleased(InputAction.CallbackContext ctx)
    {
        if (m_isChargingDash && m_dashChargeUpCoroutine != null)
        {
            StopCoroutine(m_dashChargeUpCoroutine);
            
            m_dashTargetGameObject.SetActive(false);
            
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

        // float finalDistance = (endPos - initialPos).magnitude;
        
        //do a raycast, see if we need to stop early because we're hitting a wall.
        // RaycastHit hit;
        // if (Physics.Raycast(initialPos, (endPos - initialPos).normalized, out hit, layerMask: LayerMask.GetMask(new string[]{"StaticObstacle"}), maxDistance: finalDistance))
        // {
        //     finalDistance = hit.distance;
        // }
        
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

        if (m_isBeingPushed)
        {
            StopCoroutine(m_beingPushedCoroutine);
        }
        
        m_beingPushedCoroutine = StartCoroutine(GetPushedCoroutine(dashDirection));
        m_isBeingPushed = true;

        m_onPlayerGetPushed();
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
