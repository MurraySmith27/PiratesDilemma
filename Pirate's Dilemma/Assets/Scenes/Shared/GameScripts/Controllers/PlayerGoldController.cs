using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;


public delegate void PlayerPickUpGoldEvent(int teamNum, int playerNum);
public delegate void PlayerDropGoldEvent(int teamNum, int playerNum);
public delegate void PlayerBoardBoatEvent(int teamNum, int playerNum, int boatNum);

[RequireComponent(typeof(PlayerInput), typeof(PlayerData))]
public class PlayerGoldController : MonoBehaviour
{
    public int m_goldCarried;
    
    [SerializeField] public int m_goldCapacity = 3;

    [SerializeField] private AudioSource m_goldPickupAudioSource;

    [SerializeField] private GameObject m_heldGoldGameObject;

    [SerializeField] private GameObject m_throwingTargetGameObject;

    [SerializeField] private float m_maxThrowDistance;
    
    [SerializeField] private float m_timeToGetMaxThrowRange;

    [SerializeField] private float m_throwHeightDecreaseFactor = 1f;

    [SerializeField] private GameObject m_looseGoldPrefab;

    [SerializeField] private float m_looseGoldPickupRadius;
    
    [SerializeField] private float m_goldThrowingAirTime;
    
    [SerializeField] private float m_goldThrowingPeakHeight;

    [SerializeField] private int m_trajectoryLineResolution = 10;

    [SerializeField] private Transform m_feetPosition;

    public PlayerPickUpGoldEvent m_onPlayerPickupGold;

    public PlayerDropGoldEvent m_onPlayerDropGold;
    
    public PlayerBoardBoatEvent m_onPlayerBoardBoat;
    
    private PlayerInput m_playerInput;
    private PlayerMovementController m_playerMovementController;
    
    private InputAction m_interactAction;
    private InputAction m_throwAction;

    private PlayerData m_playerData;

    private bool m_inGoldDropZone = false;
    private GameObject m_currentGoldDropzone;
    private bool m_inGoldPickupZone = false;

    private Coroutine m_throwingCoroutine;

    private bool m_throwing;

    private bool m_readyToThrow;

    void Awake()
    {
        GameTimerSystem.Instance.m_onGameStart += OnGameStart;
        GameTimerSystem.Instance.m_onGameFinish += OnGameStop;
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
        m_readyToThrow = false;
        if (m_throwingCoroutine != null)
        {
            StopCoroutine(m_throwingCoroutine);
        }
        
        m_throwing = false;
        m_heldGoldGameObject.SetActive(false);

        m_inGoldPickupZone = false;
        m_inGoldDropZone = false;
    }

    public void OnGameStop()
    {
        m_interactAction.performed -= OnInteractButtonPressed;
        m_throwAction.performed -= OnThrowButtonHeld;
        m_throwAction.canceled -= OnThrowButtonReleased;
        
        if (m_throwingCoroutine != null)
        {
            StopCoroutine(m_throwingCoroutine);
        }
    }

    public bool IsOccupied()
    {
        return m_throwing;
    }

    private void OnInteractButtonPressed(InputAction.CallbackContext ctx)
    {
        m_readyToThrow = true;
        bool pickedUpGold = false;
        if (m_goldCarried < m_goldCapacity)
        {
            List<GameObject> looseGoldInScene = GameObject.FindGameObjectsWithTag("LooseGold").ToList();
            foreach (GameObject looseGold in looseGoldInScene)
            {
                if ((looseGold.transform.position - transform.position).magnitude < m_looseGoldPickupRadius)
                {
                    
                    PickupGold();
                    pickedUpGold = true;
                    Destroy(looseGold);
                    break;
                }
            }

            if (!pickedUpGold && m_inGoldDropZone)
            {
                BoardBoat();
            }
        }
        if (m_goldCarried < m_goldCapacity && m_inGoldPickupZone && !pickedUpGold)
        {
            PickupGold();
        }
        else if (m_goldCarried != 0 && m_inGoldDropZone)
        {
            DropGold();
        }
    }

    private void BoardBoat()
    {
        GameObject boat = m_currentGoldDropzone.GetComponent<GoldDropZoneData>().m_boat;
        if (boat != null)
        {
            BoatData boatData = boat.GetComponent<BoatData>();
            if (boatData.m_numPlayersBoarded < boatData.m_playerBoardedPositions.Count && 
                boatData.m_currentTotalGoldStored > 0 && boatData.m_teamNum == m_playerData.m_teamNum)
            {
                BoatGoldController boatGoldController = boat.GetComponent<BoatGoldController>();

                boatGoldController.BoardPlayerOnBoat(this.transform);
            }

            m_onPlayerBoardBoat(m_playerData.m_teamNum, m_playerData.m_playerNum, boatData.m_boatNum);
        }
    }
    private void OnThrowButtonHeld(InputAction.CallbackContext ctx)
    {
        if (m_goldCarried > 0 && !m_playerMovementController.IsOccupied() && m_readyToThrow)
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
        m_throwingTargetGameObject.transform.position = m_feetPosition.position;

        Vector3 initialPos = m_throwingTargetGameObject.transform.position;
        
        Vector3 maxDistancePos = initialPos + transform.forward * m_maxThrowDistance;

        float heightDeltaWithFloor = transform.position.y - m_feetPosition.position.y;
        
        LineRenderer trajectoryLine = GetComponent<LineRenderer>();

        CharacterController targetCharacterController = m_throwingTargetGameObject.GetComponent<CharacterController>();
        float t = 0;
        while (true)
        {
            t = Mathf.Min(t + Time.deltaTime * m_timeToGetMaxThrowRange, 1);
            maxDistancePos = initialPos + transform.forward * m_maxThrowDistance;
          
            trajectoryLine.positionCount = m_trajectoryLineResolution;
            
            Vector3 targetPos = initialPos + (maxDistancePos - initialPos) * t;
            targetCharacterController.Move(targetPos - m_throwingTargetGameObject.transform.position);

            float currentDistance = ((maxDistancePos - initialPos) * t).magnitude / m_throwHeightDecreaseFactor;
            
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
            if (!m_throwingTargetGameObject.activeInHierarchy)
            {
                m_throwingTargetGameObject.SetActive(true);
            }
        }
    }

    private void PickupGold()
    {
        m_goldPickupAudioSource.Play();
        m_goldCarried += 1;

        m_heldGoldGameObject.SetActive(true);

        if (m_onPlayerPickupGold != null && m_onPlayerPickupGold.GetInvocationList().Length > 0)
        {
            m_onPlayerPickupGold(m_playerData.m_teamNum, m_playerData.m_playerNum);
        }

        m_readyToThrow = false;
    }
    
    private void DropGold()
    {
        GameObject boat = m_currentGoldDropzone.GetComponent<GoldDropZoneData>().m_boat;
        
        if (boat && boat.GetComponent<BoatGoldController>().m_acceptingGold)
        {
            boat.GetComponent<BoatGoldController>().AddGold(m_goldCarried, GetComponent<PlayerData>().m_teamNum);
            DropAllGold();
        }
    }

    private IEnumerator ThrowGoldCoroutine(Vector3 finalPos, GameObject looseGold)
    {
        Vector3 initialPos = m_feetPosition.position;
        
        float heightDeltaWithFloor = transform.position.y - m_feetPosition.position.y;

        float finalPosHeightDeltaWithFloor = finalPos.y - m_feetPosition.position.y;

        float horizontalDistance = (new Vector3(finalPos.x, 0, finalPos.z) - new Vector3(initialPos.x, 0, initialPos.z)).magnitude / m_throwHeightDecreaseFactor;
            
        Rigidbody looseGoldRb = looseGold.GetComponent<Rigidbody>();
        looseGoldRb.isKinematic = true;
        
        float throwGoldTime = m_goldThrowingAirTime * (horizontalDistance / m_maxThrowDistance);
        
        for (float t = 0; t < 1; t += Time.fixedDeltaTime / throwGoldTime)
        {
            yield return new WaitForFixedUpdate();
            if (looseGoldRb == null)
            {
                break;
            }
            float progress = Mathf.Asin(2f * t - 1f) / Mathf.PI + 0.5f;
            Vector3 newPos = initialPos + (finalPos - initialPos) * progress;

            if (horizontalDistance == 0)
            {
                break;
            }
            newPos.y = -(progress * horizontalDistance - horizontalDistance) * (progress*horizontalDistance + ((heightDeltaWithFloor - finalPosHeightDeltaWithFloor) / horizontalDistance)) + finalPosHeightDeltaWithFloor;
            
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
    
    public void DropAllGold()
    {
        m_heldGoldGameObject.SetActive(false);
        m_goldCarried = 0;

        m_throwing = false;
        if (m_onPlayerDropGold != null && m_onPlayerDropGold.GetInvocationList().Length > 0)
        {
            m_onPlayerDropGold(m_playerData.m_teamNum, m_playerData.m_playerNum);
        }
    }

    void OnTriggerEnter(Collider otherCollider)
    {
        if (otherCollider.gameObject.layer == LayerMask.NameToLayer("GoldDropZone"))
        {
            m_inGoldDropZone = true;
            m_currentGoldDropzone = otherCollider.gameObject;
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
            m_currentGoldDropzone = null;
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
