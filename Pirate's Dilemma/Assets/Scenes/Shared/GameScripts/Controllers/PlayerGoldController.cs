using System.Collections;
using System.Collections.Generic;
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
    
    private GameObject m_heldGoldInstance;
    
    private PlayerInput m_playerInput;
    
    private InputAction m_interactAction;
    private InputAction m_throwAction;
    private InputAction m_moveAction;

    private PlayerData m_playerData;

    private bool m_inGoldDropZone = false;
    private bool m_inGoldPickupZone = false;

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
        else if (m_goldCarried != 0 && m_inGoldDropZone)
        {
            DropGold();
        }
    }

    private void OnThrowButtonHeld(InputAction.CallbackContext ctx)
    {
        Debug.Log("throw started");
        m_throwing = true;
        //freeze player while charging throw
        m_rigidbody.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;
        m_throwingCoroutine = StartCoroutine(ExtendLandingPositionCoroutine());
    }

    private void OnThrowButtonReleased(InputAction.CallbackContext ctx)
    {
        
        if (m_throwingCoroutine != null)
        {
            StopCoroutine(m_throwingCoroutine);
            
            Vector3 targetPos = m_throwingTargetGameObject.transform.position;

            StartCoroutine(ThrowGoldCoroutine(targetPos));

            m_throwingTargetGameObject.SetActive(false);
            
            m_throwing = false;
            
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
        Vector3 maxDistancePos = initialPos + new Vector3(moveVector.x, initialPos.y, moveVector.y) * m_maxThrowDistance;

        float t = 0;
        while (true)
        {
            t = Mathf.Min(t + Time.deltaTime * m_timeToGetMaxThrowRange, 1);
            moveVector = -m_moveAction.ReadValue<Vector2>();
            maxDistancePos = initialPos + new Vector3(moveVector.x, 0, moveVector.y) * m_maxThrowDistance;
            
            m_throwingTargetGameObject.transform.position = initialPos + (maxDistancePos - initialPos) * t;
            yield return null;
        }

    }

    private void PickupGold()
    {
        m_goldPickupAudioSource.Play();
        m_goldCarried += 1;

        m_heldGoldGameObject.SetActive(true);
        m_heldGoldGameObject.transform.localScale *= m_goldPrefabScaleIncreaseFactor;
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

    private IEnumerator ThrowGoldCoroutine(Vector3 finalPos)
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
            
        GameObject looseGold = Instantiate(m_looseGoldPrefab, initialPos, Quaternion.identity);
        Rigidbody looseGoldRb = looseGold.GetComponent<Rigidbody>();
        looseGoldRb.isKinematic = true;
        for (float t = 0; t < 1; t += Time.deltaTime / m_goldThrowingAirTime)
        {
            yield return new WaitForFixedUpdate();

            float progress = Mathf.Asin(2f * t - 1f) / Mathf.PI + 0.5f;
            Vector3 newPos = initialPos + (initialPos - finalPos) * progress;

            newPos.y = -(t - distance) * (t + (heightDeltaWithFloor / distance));
            
            looseGoldRb.MovePosition(newPos);
        }

        looseGoldRb.isKinematic = false;

    }
    
    

    public GameObject MaybeFindNearestBoat()
    {
        GameObject nearestDropZoneBoat = null;
        float nearestDropZoneDistance = float.MaxValue;

        foreach (GameObject boat in GameObject.FindGameObjectsWithTag($"Team{m_playerData.m_teamNum}Boat"))
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
    }

    void OnTriggerEnter(Collider otherCollider)
    {
        if (otherCollider.gameObject.layer == LayerMask.NameToLayer("GoldDropZone"))
        {
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
