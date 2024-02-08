using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    
    private GameObject m_heldGoldInstance;
    
    private PlayerInput m_playerInput;
    
    private InputAction m_interactAction;
    private InputAction m_throwAction;
    private InputAction m_moveAction;

    private PlayerData m_playerData;

    private bool m_inGoldDropZone = false;
    private bool m_inGoldPickupZone = false;

    private bool m_inBoat = false;

    private Rigidbody m_rigidbody;

    private Coroutine m_throwingCoroutine;
    
    private bool m_throwing;

    void Awake()
    {
        GameTimerSystem.Instance.m_onGameStart += OnGameStart;
    }
    
    void OnGameStart()
    {   
        //Assigning Callbacks
        m_goldCarried = 0;
        m_playerInput = GetComponent<PlayerInput>();
        m_playerData = GetComponent<PlayerData>();

        m_interactAction = m_playerInput.actions["Interact"];
        m_interactAction.performed += OnInteractButtonPressed;

        m_throwAction = m_playerInput.actions["Throw"];
        m_throwAction.performed += OnThrowButtonHeld;
        m_throwAction.canceled += OnThrowButtonReleased;

        m_moveAction = m_playerInput.actions["Move"];
        
        m_throwing = false;

        m_rigidbody = GetComponent<Rigidbody>();
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

            foreach (GameObject looseGold in looseGoldInScene)
            {
                if ((looseGold.transform.position - transform.position).magnitude < 2f)
                {
                    PickupGold();
                    
                    Destroy(looseGold);
                    break;
                }
            }
        }
        else if (m_goldCarried != 0 && m_inGoldDropZone)
        {
            DropGold();
        }
        else if (m_goldCarried == 0 && m_inGoldDropZone)
        {
            ToSailBoat();
        }
    }
    
    private void ToSailBoat()
    {
        GameObject boat = MaybeFindNearestBoat();
        if (boat != null)
        {
            BoatController boatController = boat.GetComponent<BoatController>();
            if (boatController != null && boatController.AddPlayer())
            {
                // Move player to the boat's player spot
                this.transform.position = boatController.playerSpot.position;
                this.transform.SetParent(boatController.playerSpot); // Optionally parent the player to the boat

                // Here you can disable player movement or other components as necessary
            }
            else
            {
                Debug.Log("Boat is full or no boat controller found.");
            }
        }
        
    }

    private void OnThrowButtonHeld(InputAction.CallbackContext ctx)
    {
        if (m_goldCarried > 0)
        {
            m_throwing = true;
            //freeze player while charging throw
            m_rigidbody.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;
            LineRenderer trajectoryLine = GetComponent<LineRenderer>();
            trajectoryLine.enabled = true;
            m_throwingCoroutine = StartCoroutine(ExtendLandingPositionCoroutine());
        }
    }

    private void OnThrowButtonReleased(InputAction.CallbackContext ctx)
    {
        
        if (m_throwingCoroutine != null && m_throwing)
        {
            StopCoroutine(m_throwingCoroutine);
            
            LineRenderer trajectoryLine = GetComponent<LineRenderer>();
            trajectoryLine.enabled = false;
            
            Vector3 targetPos = m_throwingTargetGameObject.transform.position;
            
            
            GameObject looseGold = Instantiate(m_looseGoldPrefab, transform.position, Quaternion.identity);
            
            Coroutine throwGoldCoroutine = StartCoroutine(ThrowGoldCoroutine(targetPos, looseGold));

            looseGold.GetComponent<LooseGoldController>().m_onLooseGoldCollision +=
                () => { StopCoroutine(throwGoldCoroutine); };
            
            m_throwingTargetGameObject.SetActive(false);
            
            m_throwing = false;

            DropAllGold();
            
            //unlock player 
            m_rigidbody.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        }

    }

    private IEnumerator ExtendLandingPositionCoroutine()
    {
        m_throwingTargetGameObject.SetActive(true);
        m_throwingTargetGameObject.transform.position = transform.position;

        Vector3 initialPos = m_throwingTargetGameObject.transform.position;

        Vector2 moveVector = m_moveAction.ReadValue<Vector2>();
        if (moveVector.magnitude <= 0.01f)
        {
            moveVector = new Vector2(1f, 0f);
        }
        Vector3 maxDistancePos = initialPos + new Vector3(moveVector.x, 0, moveVector.y) * m_maxThrowDistance;
        
        float heightDeltaWithFloor = transform.position.y;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, new Vector3(0, -1, 0), maxDistance: 0f, hitInfo: out hit,
                layerMask: ~LayerMask.NameToLayer("Floor")))
        {
            heightDeltaWithFloor = hit.distance;
        }
        
        
        LineRenderer trajectoryLine = GetComponent<LineRenderer>();

        float t = 0;
        while (true)
        {
            t = Mathf.Min(t + Time.deltaTime * m_timeToGetMaxThrowRange, 1);
            moveVector = -m_moveAction.ReadValue<Vector2>();
            maxDistancePos = initialPos + new Vector3(moveVector.x, 0, moveVector.y) * m_maxThrowDistance;
            
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
        Vector3 initialPos = transform.position;
        float initialHeight = initialPos.y;
        RaycastHit hit;
        float heightDeltaWithFloor = initialHeight;
        if (Physics.Raycast(transform.position, new Vector3(0, -1, 0), maxDistance: 0f, hitInfo: out hit,
                layerMask: ~LayerMask.NameToLayer("Floor")))
        {
            heightDeltaWithFloor = hit.distance;
        }

        float distance = (new Vector3(finalPos.x, 0, finalPos.z) - new Vector3(initialPos.x, 0, initialPos.z)).magnitude;
            
        Rigidbody looseGoldRb = looseGold.GetComponent<Rigidbody>();
        looseGoldRb.isKinematic = true;
        looseGold.layer = LayerMask.NameToLayer("AirbornLooseGold");
        
        for (float t = 0; t < 1; t += Time.deltaTime / m_goldThrowingAirTime)
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
