using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;


[RequireComponent(typeof(PlayerInput), typeof(PlayerData))]
public class PlayerGoldController : MonoBehaviour
{
    public int m_goldCarried;
    
    [SerializeField] public int m_goldCapacity = 3;

    [SerializeField] float m_goldPrefabScaleIncreaseFactor = 1.1f;
    
    [SerializeField] private AudioSource m_goldPickupAudioSource;

    [SerializeField] private GameObject m_heldGoldGameObject;

    [SerializeField] private GameObject m_throwingTargetGameObject;

    [SerializeField] private float m_maxThrowDistance;
    
    [SerializeField] private float m_timeToGetMaxThrowRange;

    [SerializeField] private GameObject m_looseGoldPrefab;
    
    [SerializeField] private float m_goldThrowingAirTime;
    
    [SerializeField] private float m_goldThrowingPeakHeight;

    [SerializeField] private int m_trajectoryLineResolution = 10;

    [SerializeField] private Transform m_feetPosition;
    
    private GameObject m_heldGoldInstance;
    
    private PlayerInput m_playerInput;
    private PlayerMovementController m_playerMovementController;
    
    private InputAction m_interactAction;
    private InputAction m_throwAction;
    private InputAction m_moveAction;

    private PlayerData m_playerData;

    private bool m_inGoldDropZone = false;
    private bool m_inGoldPickupZone = false;

    private Coroutine m_throwingCoroutine;

    private bool m_throwing;

    void Awake()
    {
        GameTimerSystem.Instance.m_onGameStart += OnGameStart;
    }
    
    public void OnGameStart()
    {   
        //Assigning Callbacks
        m_goldCarried = 0;
        m_playerInput = GetComponent<PlayerInput>();
        m_playerData = GetComponent<PlayerData>();
        m_playerMovementController = GetComponent<PlayerMovementController>();
        m_playerMovementController.m_onPlayerGetPushed += OnGetPushed;

        m_interactAction = m_playerInput.actions["Interact"];
        m_interactAction.performed += OnInteractButtonPressed;

        m_throwAction = m_playerInput.actions["Throw"];
        m_throwAction.performed += OnThrowButtonHeld;
        m_throwAction.canceled += OnThrowButtonReleased;
        m_throwAction.Disable(); //starts disabled until we pick up gold.

        m_moveAction = m_playerInput.actions["Move"];
        
        m_throwing = false;
    }

    public void OnGameStop()
    {
        m_interactAction.performed -= OnInteractButtonPressed;
        m_throwAction.performed -= OnThrowButtonHeld;
        m_throwAction.canceled -= OnThrowButtonReleased;
    }

    public bool IsOccupied()
    {
        return m_throwing;
    }

    private void OnInteractButtonPressed(InputAction.CallbackContext ctx)
    {
        if (m_goldCarried < m_goldCapacity && m_inGoldPickupZone)
        {
            PickupGold();
        }
        else if (m_goldCarried < m_goldCapacity)
        {
            List<GameObject> looseGoldInScene = GameObject.FindGameObjectsWithTag("LooseGold").ToList();
            bool pickedUpGold = false;
            foreach (GameObject looseGold in looseGoldInScene)
            {
                if ((looseGold.transform.position - transform.position).magnitude < 2f)
                {
                    PickupGold();
                    pickedUpGold = true;
                    Destroy(looseGold);
                    break;
                }
            }

            if (!pickedUpGold)
            {
                BoardBoat();
            }
        }
        else if (m_goldCarried != 0 && m_inGoldDropZone)
        {
            DropGold();
        }
    }

    private void BoardBoat()
    {
        GameObject boat = MaybeFindNearestBoat();
        if (boat != null)
        {
            BoatData boatData = boat.GetComponent<BoatData>();
            if (boatData.m_numPlayersBoarded < boatData.m_playerBoardedPositions.Count && 
                boatData.m_currentTotalGoldStored > 0 && boatData.m_teamNum == m_playerData.m_teamNum)
            {

                BoatGoldController boatGoldController = boat.GetComponent<BoatGoldController>();

                boatGoldController.BoardPlayerOnBoat(this.transform);
            }
        }
    }
    private void OnThrowButtonHeld(InputAction.CallbackContext ctx)
    {
        if (m_goldCarried > 0 && !m_playerMovementController.IsOccupied())
        {
            m_throwing = true;
            //freeze player while charging throw
            LineRenderer trajectoryLine = GetComponent<LineRenderer>();
            trajectoryLine.enabled = true;
            m_throwingCoroutine = StartCoroutine(ExtendLandingPositionCoroutine());
        }
    }

    public GameObject SpawnLooseGold(bool isThrowing)
    {

        GameObject looseGoldPrefab = Instantiate(m_looseGoldPrefab, transform.position, Quaternion.identity);
        
        if (isThrowing)
        {
            looseGoldPrefab.layer = LayerMask.NameToLayer("AirbornLooseGold");
        }

        return looseGoldPrefab;
    }

    private void OnThrowButtonReleased(InputAction.CallbackContext ctx)
    {
        
        if (m_throwingCoroutine != null && m_throwing)
        {
            StopCoroutine(m_throwingCoroutine);
            
            LineRenderer trajectoryLine = GetComponent<LineRenderer>();
            trajectoryLine.enabled = false;
            
            
            Vector3 targetPos = m_throwingTargetGameObject.transform.position;
            GameObject looseGold = SpawnLooseGold(true);
            
            Coroutine throwGoldCoroutine = StartCoroutine(ThrowGoldCoroutine(targetPos, looseGold));

            looseGold.GetComponent<LooseGoldController>().m_onLooseGoldCollision +=
                () =>
                {
                    looseGold.GetComponent<Rigidbody>().isKinematic = false;
                    StopCoroutine(throwGoldCoroutine); 
                };
            
            m_throwingTargetGameObject.SetActive(false);
            
            m_throwing = false;

            DropAllGold();
        }

    }

    private IEnumerator ExtendLandingPositionCoroutine()
    {
        m_throwingTargetGameObject.SetActive(true);
        m_throwingTargetGameObject.transform.position = m_feetPosition.position;

        Vector3 initialPos = m_throwingTargetGameObject.transform.position;

        Vector2 moveVector = m_moveAction.ReadValue<Vector2>();
        if (moveVector.magnitude <= 0.01f)
        {
            moveVector = new Vector2(1f, 0f);
        }
        Vector3 maxDistancePos = initialPos + new Vector3(moveVector.x, 0, moveVector.y) * m_maxThrowDistance;

        float heightDeltaWithFloor = transform.position.y - m_feetPosition.position.y;
        
        LineRenderer trajectoryLine = GetComponent<LineRenderer>();

        float t = 0;
        while (true)
        {
            t = Mathf.Min(t + Time.deltaTime * m_timeToGetMaxThrowRange, 1);
            maxDistancePos = initialPos + new Vector3(moveVector.x, 0, moveVector.y) * m_maxThrowDistance;
            
            Vector2 newMoveVector = -m_moveAction.ReadValue<Vector2>();
            if (newMoveVector.magnitude != 0)
            {
                moveVector = newMoveVector;
            }
            trajectoryLine.positionCount = m_trajectoryLineResolution;
            
            Vector3 targetPos = initialPos + (maxDistancePos - initialPos) * t;
            m_throwingTargetGameObject.transform.position = targetPos;

            float currentDistance = ((maxDistancePos - initialPos) * t).magnitude;
            
            List<Vector3> linePositions = new List<Vector3>();
            for (int i = 0; i < m_trajectoryLineResolution; i++)
            {
                //progress along line
                float t2 = i / (float)m_trajectoryLineResolution;
                float currentHeight = -(t2 * currentDistance - currentDistance) * (t2 * currentDistance + (heightDeltaWithFloor / currentDistance));
                Vector3 currentPositionAlongLine = initialPos + (targetPos - initialPos) * t2;
                linePositions.Add(new Vector3(currentPositionAlongLine.x, currentHeight, currentPositionAlongLine.z));
            }
            
            trajectoryLine.SetPositions(linePositions.ToArray());
            
            yield return null;
        }
    }

    private void PickupGold()
    {
        m_goldPickupAudioSource.Play();
        m_goldCarried += 1;

        m_heldGoldGameObject.SetActive(true);

        m_interactAction.Disable();
        
        m_throwAction.Enable();
    }
    
    private void DropGold()
    {
        GameObject boat = MaybeFindNearestBoat();
        
        if (boat && boat.GetComponent<BoatGoldController>().m_acceptingGold)
        {
            boat.GetComponent<BoatGoldController>().AddGold(m_goldCarried, GetComponent<PlayerData>().m_teamNum);
            DropAllGold();
        }
    }

    private IEnumerator ThrowGoldCoroutine(Vector3 finalPos, GameObject looseGold)
    {
        Vector3 initialPos = m_feetPosition.position;
        
        float initialHeight = initialPos.y;
        RaycastHit hit;
        float heightDeltaWithFloor = transform.position.y - m_feetPosition.position.y;

        float distance = (new Vector3(finalPos.x, 0, finalPos.z) - new Vector3(initialPos.x, 0, initialPos.z)).magnitude;
            
        Rigidbody looseGoldRb = looseGold.GetComponent<Rigidbody>();
        looseGoldRb.isKinematic = true;
        
        float throwGoldTime = m_goldThrowingAirTime * (distance / m_maxThrowDistance);
        
        for (float t = 0; t < 1; t += Time.fixedDeltaTime / throwGoldTime)
        {
            yield return new WaitForFixedUpdate();
            if (looseGoldRb == null)
            {
                break;
            }
            float progress = Mathf.Asin(2f * t - 1f) / Mathf.PI + 0.5f;
            Vector3 newPos = initialPos + (finalPos - initialPos) * progress;

            if (distance == 0)
            {
                break;
            }
            newPos.y = -(progress * distance - distance) * (progress*distance + (heightDeltaWithFloor / distance));
            
            looseGoldRb.MovePosition(newPos);
        }
        
        if (looseGoldRb != null)
        {
            looseGoldRb.isKinematic = false;
            looseGold.layer = LayerMask.NameToLayer("LooseGold");

        }

    }

    private void OnGetPushed()
    {
        if (m_throwing)
        {
            StopCoroutine(m_throwingCoroutine);
            m_throwing = false;
        }
        
        if (m_goldCarried > 0)
        {
            SpawnLooseGold(false);
        }
        DropAllGold();
    }
    
    private GameObject MaybeFindNearestBoat()
    {
        GameObject nearestDropZoneBoat = null;
        float nearestDropZoneDistance = float.MaxValue;
        List<GameObject> allBoats = GameObject.FindGameObjectsWithTag($"Team1Boat")
            .Concat(GameObject.FindGameObjectsWithTag($"Team2Boat")).ToList();
        foreach (GameObject boat in allBoats)
        {
            Transform dropZoneTransform = boat.GetComponent<BoatGoldController>().m_goldDropZone.transform;
            float distanceToDropZone = (this.transform.position - dropZoneTransform.position).magnitude;
            if (distanceToDropZone < 10 && distanceToDropZone < nearestDropZoneDistance)
            {
                nearestDropZoneBoat = boat;
                nearestDropZoneDistance = distanceToDropZone;
            }
        }
        
        return nearestDropZoneBoat;
    }
    
    public void DropAllGold()
    {
        m_heldGoldGameObject.SetActive(false);
        m_goldCarried = 0;

        m_throwing = false;
        m_interactAction.Enable();
        m_throwAction.Disable();
    }

    void OnTriggerEnter(Collider otherCollider)
    {
        if (otherCollider.gameObject.layer == LayerMask.NameToLayer("GoldDropZone"))
        {
            if (m_goldCarried > 0)
            {
                m_interactAction.Enable();
                m_throwAction.Disable();
            }

            m_inGoldDropZone = true;
        }
        
        if (otherCollider.gameObject.layer == LayerMask.NameToLayer("GoldPickupZone"))
        {
            m_inGoldPickupZone = true;
        }
    }
    
    void OnTriggerExit(Collider otherCollider)
    {
        if (otherCollider.gameObject.layer == LayerMask.NameToLayer("GoldDropZone"))
        {
            if (m_goldCarried > 0)
            {
                m_interactAction.Disable();
                m_throwAction.Enable();
            }

            m_inGoldDropZone = false;
        }
        
        if (otherCollider.gameObject.layer == LayerMask.NameToLayer("GoldPickupZone"))
        {
            m_inGoldPickupZone = false;
        }
    }
    
    void OnEnable()
    {
        if (m_interactAction != null)
        {
            m_interactAction.Enable();
        }
    }

    void OnDisable()
    {
        if (m_interactAction != null)
        {
            m_interactAction.Disable();
        }
    }
    
} 
