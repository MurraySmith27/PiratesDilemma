using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public delegate void PlayerPickUpBombEvent(int teamNum, int playerNum);
public delegate void PlayerDropBombEvent(int teamNum, int playerNum);

public delegate void PlayerDropBombOntoBoatEvent();
public delegate void PlayerBoardBoatEvent(int teamNum, int playerNum, int boatNum);
public delegate void PlayerStartedThrowChargeEvent();
public delegate void PlayerStartedThrowEvent();
// public delegate void PlayerEnterGoldPickupZoneEvent(int teamNum, int playerNum);
// public delegate void PlayerEnterGoldDropZoneEvent(int teamNum, int playerNum);
// public delegate void PlayerExitGoldPickupZoneEvent(int teamNum, int playerNum);
// public delegate void PlayerExitGoldDropZoneEvent(int teamNum, int playerNum);

[RequireComponent(typeof(PlayerInput), typeof(PlayerData))]
public class PlayerItemController : MonoBehaviour
{
    [FormerlySerializedAs("m_goldCapacity")] [SerializeField] public int m_bombCapacity = 1;

    [SerializeField] private Transform m_heldBombPositionTransform = null;
    
    private GameObject m_heldBombGameObject = null;
    
    [SerializeField] private GameObject m_heldBarrelGameObject;

    [SerializeField] private GameObject m_throwingTargetGameObject;

    [SerializeField] private float m_maxThrowDistance;
    
    [SerializeField] private float m_timeToGetMaxThrowRange;

    [SerializeField] private float m_throwHeightDecreaseFactor = 1f;

    [FormerlySerializedAs("m_looseGoldPrefab")] [SerializeField] private GameObject m_looseBombPrefab;

    [FormerlySerializedAs("m_looseGoldPickupRadius")] public float m_looseItemPickupRadius;
    
    [FormerlySerializedAs("m_goldThrowingAirTime")] [SerializeField] private float m_bombThrowingAirTime;
    
    [FormerlySerializedAs("m_goldThrowingPeakHeight")] [SerializeField] private float m_bombThrowingPeakHeight;

    [SerializeField] private int m_trajectoryLineResolution = 10;

    [SerializeField] private Transform m_feetPosition;
    
    [SerializeField] private float m_getOffBoatInputThreshold = 0.5f;

    [SerializeField] private float m_movingThrowMultiplier;
    
    [SerializeField] private float m_dashingThrowMultiplier;
    
    public PlayerPickUpBombEvent m_onPlayerPickupBomb;

    public PlayerDropBombEvent m_onPlayerDropBomb;
    
    public PlayerStartedThrowChargeEvent m_onPlayerStartThrowCharge;
    
    public PlayerStartedThrowEvent m_onPlayerStartThrow;

    // public PlayerEnterGoldPickupZoneEvent m_onPlayerEnterGoldPickupZone;
    //
    // public PlayerExitGoldPickupZoneEvent m_onPlayerExitGoldPickupZone;
    //
    // public PlayerEnterGoldDropZoneEvent m_onPlayerEnterGoldDropZone;
    //
    // public PlayerExitGoldDropZoneEvent m_onPlayerExitGoldDropZone;
    
    
    private PlayerInput m_playerInput;
    private PlayerMovementController m_playerMovementController;

    private InputAction m_moveAction;
    private InputAction m_interactAction;
    private InputAction m_throwAction;

    private PlayerData m_playerData;
    //
    // private bool m_inFriendlyGoldDropZone;
    //
    // private bool m_inEnemyGoldDropZone;
    //
    // private bool m_inGoldDropZone
    // {
    //     get
    //     {
    //         return m_inFriendlyGoldDropZone || m_inEnemyGoldDropZone;
    //     }
    // }
    //
    // private GameObject m_currentGoldDropzone;
    // private bool m_inGoldPickupZone = false;

    private Coroutine m_throwingCoroutine;

    private bool m_throwing;

    private bool m_readyToThrow;

    public bool m_barrelInHand
    {
        get;
        private set;
    }

    private bool m_isBoardedOnBoat;

    private GameObject m_boardedBoat;

    private Transform m_playerOriginalParent;
    

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameTimerSystem.Instance.m_onGameStart += OnGameStart;
        GameTimerSystem.Instance.m_onGameFinish += OnGameStop;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        m_throwing = false;
        m_heldBarrelGameObject.SetActive(false);
        Destroy(m_heldBombGameObject);
    }
    
    public void OnGameStart()
    {   
        //Assigning Callbacks
        m_playerInput = GetComponent<PlayerInput>();
        m_playerData = GetComponent<PlayerData>();
        m_playerMovementController = GetComponent<PlayerMovementController>();
        m_playerMovementController.m_onPlayerGetPushed += OnGetPushed;
        m_playerMovementController.m_onPlayerDie += OnPlayerDie;
        
        m_playerData.m_bombsCarried = 0;
        
        Destroy(m_heldBombGameObject);

        m_interactAction = m_playerInput.actions["Interact"];
        m_interactAction.performed += OnInteractButtonPressed;

        m_throwAction = m_playerInput.actions["Throw"];
        // m_throwAction.performed += OnThrowButtonHeld;
        // m_throwAction.canceled += OnThrowButtonReleased;

        m_moveAction = m_playerInput.actions["Move"];
        // m_moveAction.performed += OnMoveActionPerformed;
        
        m_readyToThrow = false;
        if (m_throwingCoroutine != null)
        {
            StopCoroutine(m_throwingCoroutine);
            m_throwingTargetGameObject.SetActive(false);
        }

        m_playerOriginalParent = transform.parent;
        
        m_throwing = false;
        m_heldBarrelGameObject.SetActive(false);
        
        m_isBoardedOnBoat = false;
        //
        // m_inGoldPickupZone = false;
        // m_inFriendlyGoldDropZone = false;
        // m_inEnemyGoldDropZone = false;
    }

    public void OnGameStop()
    {
        m_interactAction.performed -= OnInteractButtonPressed;
        // m_throwAction.performed -= OnThrowButtonHeld;
        // m_throwAction.canceled -= OnThrowButtonReleased;
        // m_moveAction.performed -= OnMoveActionPerformed;
        
        if (m_throwingCoroutine != null)
        {
            StopCoroutine(m_throwingCoroutine);
        }
    }

    public bool IsOccupied()
    {
        return m_throwing || m_isBoardedOnBoat;
    }
    
    // private void OnMoveActionPerformed(InputAction.CallbackContext ctx)
    // {
    //     if (m_isBoardedOnBoat)
    //     {
    //         Vector2 boatDismountDirection =
    //             m_boardedBoat.GetComponent<BoatData>().m_boatDismountDirection.normalized;
    //         if (Vector2.Dot(boatDismountDirection, m_moveAction.ReadValue<Vector2>()) > m_getOffBoatInputThreshold)
    //         {
    //
    //             UnboardBoat(m_playerOriginalParent, Vector3.zero, useDefaultPosition: true);
    //         }
    //     }
    //
    // }

    private void OnInteractButtonPressed(InputAction.CallbackContext ctx)
    {
        m_readyToThrow = true;
        bool pickedUpItem = false;
        
        if (m_playerData.m_bombsCarried < m_bombCapacity && !m_barrelInHand)
        {
            List<GameObject> itemsInScene = GameObject.FindGameObjectsWithTag("LooseBomb").ToList();
            itemsInScene.AddRange(GameObject.FindGameObjectsWithTag("Barrel").ToList()); // Include barrels in the items list

            foreach (GameObject item in itemsInScene)
            {
                if ((item.transform.position - transform.position).magnitude < m_looseItemPickupRadius && m_playerData.m_bombsCarried == 0)
                {
                    if(item.CompareTag("Barrel"))
                    {
                        PickUpBarrel();
                        pickedUpItem = true;
                        Destroy(item);
                        break;
                    }
                    else
                    {
                        BombController bombController = item.GetComponent<BombController>();
                        bool isLit = bombController != null && bombController.m_isLit;
                        PickupBomb(item);
                        pickedUpItem = true;
                        break;
                    }

                }
            }
        }
        else if (m_playerData.m_bombsCarried > 0)
        {
            OnThrowButtonHeld(new InputAction.CallbackContext());
        }
        // if (!pickedUpItem)
        // {
        //     if (m_playerData.m_bombsCarried < m_bombCapacity && m_inGoldPickupZone)
        //     {
        //         PickupGold();
        //     }
        //     else if (m_playerData.m_bombsCarried != 0 && m_inFriendlyGoldDropZone)
        //     {
        //         DropGold();
        //     }
        //     else if (m_barrelInHand && m_inEnemyGoldDropZone)
        //     {
        //         DropBarrel();
        //     }
        // }
    }

    private void OnPlayerDie(int playerNum)
    {
        DropAllBombs(false);
    }
    
    // public void BoardBoat(GameObject boat)
    // {
    //     if (boat != null)
    //     {
    //         BoatData boatData = boat.GetComponent<BoatData>();
    //         if (boatData.m_numPlayersBoarded < boatData.m_playerBoardedPositions.Count && 
    //             boatData.m_currentTotalGoldStored > 0 && boatData.m_teamNum == m_playerData.m_teamNum)
    //         {
    //             BoatGoldController boatGoldController = boat.GetComponent<BoatGoldController>();
    //
    //             boatGoldController.BoardPlayerOnBoat(this.transform);
    //         }
    //
    //         m_isBoardedOnBoat = true;
    //         GetComponent<Collider>().enabled = false;
    //
    //         m_boardedBoat = boat;
    //         
    //         m_onPlayerBoardBoat(m_playerData.m_teamNum, m_playerData.m_playerNum, boatData.m_boatNum);
    //     }
    // }

    // public void UnboardBoat(Transform newParent, Vector3 playerDismountPosition, bool useDefaultPosition = false)
    // {
    //     m_isBoardedOnBoat = false;
    //     int boatNum = m_boardedBoat.GetComponent<BoatData>().m_boatNum;
    //     m_boardedBoat.GetComponent<BoatGoldController>().DismountPlayerFromBoat(this.transform, newParent, playerDismountPosition, useDefaultPosition);
    //     GetComponent<Collider>().enabled = true;
    //     m_boardedBoat = null;
    //
    //     m_onPlayerGetOffBoat(m_playerData.m_teamNum, m_playerData.m_playerNum, boatNum);
    // }
    
    private void OnThrowButtonHeld(InputAction.CallbackContext ctx)
    {
        if (m_playerData.m_bombsCarried > 0 && !m_playerMovementController.IsOccupied() && m_readyToThrow)
        {
            m_throwing = true;
            //freeze player while charging throw
            LineRenderer trajectoryLine = GetComponent<LineRenderer>();
            trajectoryLine.enabled = true;
            // m_throwingCoroutine = StartCoroutine(ExtendLandingPositionCoroutine());
            
            m_throwingTargetGameObject.SetActive(true);
            
            CharacterController targetCharacterController = m_throwingTargetGameObject.GetComponent<CharacterController>();
            
            m_onPlayerStartThrowCharge();

            float throwDistance = m_maxThrowDistance;
            
            float throwMultiplier = 1f;
            if (m_playerMovementController.m_isDashing)
            {
                throwDistance *= m_dashingThrowMultiplier;
            }
            else if (m_playerMovementController.m_isMoving && !m_playerMovementController.m_isDashing)
            {
                throwDistance *= m_movingThrowMultiplier;
            }
            
            Vector3 targetPos = m_feetPosition.transform.position + transform.forward * throwDistance;
            
            GameObject looseBomb = m_heldBombGameObject;

            BombController bombController = looseBomb.GetComponent<BombController>();
            
            bombController.m_wasThrown = true;
            
            Coroutine throwBombCoroutine = StartCoroutine(ThrowBombCoroutine(targetPos, looseBomb));

            m_onPlayerStartThrow();

            looseBomb.GetComponent<BombController>().m_onLooseBombCollision +=
                () =>
                {
                    looseBomb.GetComponent<Rigidbody>().isKinematic = false;
                    StopCoroutine(throwBombCoroutine);
                };

            targetCharacterController.enabled = false;
            m_throwingTargetGameObject.transform.position = m_feetPosition.transform.position;
            targetCharacterController.enabled = true;

            m_throwingTargetGameObject.SetActive(false);
            
            m_throwing = false;

            DropAllBombs(true);
        }
    }

    private void PickupBomb(GameObject item)
    {
        m_playerData.m_bombsCarried++;

        m_heldBombGameObject = item;
        BombController bombController = m_heldBombGameObject.GetComponent<BombController>();

        bombController.m_onBombExplode += OnBombExplodeInHand;
        bombController.m_lastHeldTeamNum = -1;

        item.GetComponent<Rigidbody>().isKinematic = true;
        
        item.transform.position = m_heldBombPositionTransform.position;
        item.transform.LookAt(Vector3.up);
        item.transform.parent = m_heldBombPositionTransform.parent;
        if (m_onPlayerPickupBomb != null && m_onPlayerPickupBomb.GetInvocationList().Length > 0)
        {
            m_onPlayerPickupBomb(m_playerData.m_teamNum, m_playerData.m_playerNum);
        }
        
        m_readyToThrow = false;
    }

    private void OnBombExplodeInHand()
    {

        m_playerData.m_bombsCarried = 0;
        
        
        m_heldBombGameObject = null;
        
        if (m_onPlayerDropBomb != null && m_onPlayerDropBomb.GetInvocationList().Length > 0)
        {
            m_onPlayerDropBomb(m_playerData.m_teamNum, m_playerData.m_playerNum);
        }
    }
    
    private void PickUpBarrel()
    {
        m_barrelInHand = true;
        m_heldBarrelGameObject.SetActive(true);
        
        // if (m_onPlayerPickupBomb != null && m_onPlayerPickupBomb.GetInvocationList().Length > 0)
        // {
        //     m_onPlayerPickupBomb(m_playerData.m_teamNum, m_playerData.m_playerNum);
        // }
    
        m_readyToThrow = false;
    }
    
    // private void DropGold()
    // {
    //     GameObject boat = m_currentGoldDropzone.GetComponent<GoldDropZoneData>().m_boat;
    //     
    //     if (boat && boat.GetComponent<BoatGoldController>().m_acceptingGold)
    //     {
    //         boat.GetComponent<BoatGoldController>().AddGold(m_playerData.m_bombsCarried, GetComponent<PlayerData>().m_teamNum);
    //
    //         if (m_onPlayerDropBombOntoBoat != null && m_onPlayerDropBombOntoBoat.GetInvocationList().Length > 0)
    //         {
    //             m_onPlayerDropBombOntoBoat();
    //         }
    //         DropAllGold();
    //     }
    // }
    
    // private void DropBarrel()
    // {
    //     GameObject boat = m_currentGoldDropzone.GetComponent<GoldDropZoneData>().m_boat;
    //     
    //     if (boat && boat.GetComponent<BoatGoldController>().m_acceptingGold)
    //     {
    //         boat.GetComponent<BoatGoldController>().AddGold(Int32.MaxValue, GetComponent<PlayerData>().m_teamNum); // sinks with a lot of gold
    //         DropAllGold();
    //         m_barrelInHand = false;
    //         m_heldBarrelGameObject.SetActive(false);
    //     
    // }

    private IEnumerator ThrowBombCoroutine(Vector3 finalPos, GameObject looseBomb)
    {

        float throwMultiplier = 1f;
        if (m_playerMovementController.m_isDashing)
        {
            throwMultiplier = m_dashingThrowMultiplier;
        }
        else if (m_playerMovementController.m_isMoving && !m_playerMovementController.m_isDashing)
        {
            throwMultiplier = m_movingThrowMultiplier;
        }
        
        Vector3 initialPos = m_feetPosition.position;
        
        float heightDeltaWithFloor = transform.position.y - m_feetPosition.position.y;

        float finalPosHeightDeltaWithFloor = finalPos.y - m_feetPosition.position.y;

        float horizontalDistance = (new Vector3(finalPos.x, 0, finalPos.z) - new Vector3(initialPos.x, 0, initialPos.z)).magnitude / m_throwHeightDecreaseFactor;
            
        Rigidbody looseBombRb = looseBomb.GetComponent<Rigidbody>();
        looseBombRb.isKinematic = true;
        
        float throwBombTime = m_bombThrowingAirTime * (horizontalDistance / (m_maxThrowDistance * throwMultiplier));
        
        for (float t = 0; t < 1; t += Time.fixedDeltaTime / throwBombTime)
        {
            yield return new WaitForFixedUpdate();
            if (looseBombRb == null)
            {
                break;
            }
            float progress = Mathf.Asin(2f * t - 1f) / Mathf.PI + 0.5f;
            Vector3 newPos = initialPos + (finalPos - initialPos) * progress;

            if (horizontalDistance == 0)
            {
                break;
            }
            newPos.y = -((progress * horizontalDistance - horizontalDistance) * (progress*horizontalDistance + ((heightDeltaWithFloor - finalPosHeightDeltaWithFloor) / horizontalDistance)) + finalPosHeightDeltaWithFloor) / (throwMultiplier);
            
            looseBombRb.MovePosition(newPos);
        }
        
        if (looseBombRb != null)
        {
            looseBombRb.isKinematic = false;
            looseBomb.layer = LayerMask.NameToLayer("LooseBomb");
        }

    }

    private void OnGetPushed(Vector3 contactPosition)
    {
        if (m_throwing)
        {
            StopCoroutine(m_throwingCoroutine);
            m_throwing = false;
            m_throwingTargetGameObject.SetActive(false);
        }
        
        DropAllBombs(false);
    }
    
    public void DropAllBombs(bool isThrown)
    {
        if (m_playerData.m_bombsCarried > 0)
        {
            m_playerData.m_bombsCarried = 0;

            BombController bombController = m_heldBombGameObject.GetComponent<BombController>();
            bombController.m_onBombExplode -= OnBombExplodeInHand;
            bombController.m_lastHeldTeamNum = m_playerData.m_teamNum;

            m_heldBombGameObject.GetComponent<Rigidbody>().isKinematic = isThrown;

            m_heldBombGameObject.transform.parent = null;
            m_heldBombGameObject.transform.position = m_feetPosition.position;
            m_heldBombGameObject = null;

            m_throwing = false;
            if (m_onPlayerDropBomb != null && m_onPlayerDropBomb.GetInvocationList().Length > 0)
            {
                m_onPlayerDropBomb(m_playerData.m_teamNum, m_playerData.m_playerNum);
            }
        }
    }


    void OnTriggerStay(Collider otherCollider)
    {
        // if (otherCollider.gameObject.layer == LayerMask.NameToLayer("GoldDropZone"))
        // {
        //     if (!m_inGoldDropZone)
        //     {
        //         int boatTeamNum = otherCollider.gameObject.GetComponent<GoldDropZoneData>().m_boat
        //             .GetComponent<BoatData>()
        //             .m_teamNum;
        //         if (boatTeamNum == m_playerData.m_teamNum)
        //         {
        //             m_inFriendlyGoldDropZone = true;
        //         }
        //         else
        //         {
        //             m_inEnemyGoldDropZone = true;
        //         }
        //
        //         m_currentGoldDropzone = otherCollider.gameObject;
        //         if (m_onPlayerEnterGoldDropZone != null && m_onPlayerEnterGoldDropZone.GetInvocationList().Length > 0)
        //         {
        //             m_onPlayerEnterGoldDropZone(m_playerData.m_teamNum, m_playerData.m_playerNum);
        //         }
        //     }
        // }
        //
        // if (otherCollider.gameObject.layer == LayerMask.NameToLayer("GoldPickupZone"))
        // {
        //     if (!m_inGoldPickupZone)
        //     {
        //         m_inGoldPickupZone = true;
        //         if (m_onPlayerEnterGoldPickupZone != null && m_onPlayerEnterGoldPickupZone.GetInvocationList().Length > 0)
        //         {
        //             m_onPlayerEnterGoldPickupZone(m_playerData.m_teamNum, m_playerData.m_playerNum);
        //         }
        //     }
        // }
    }
    
    void OnTriggerEnter(Collider otherCollider)
    {
        // if (otherCollider.gameObject.layer == LayerMask.NameToLayer("Boat") && otherCollider.gameObject.GetComponent<BoatData>().m_currentTotalGoldStored > 0)
        // {
        //     BoardBoat(otherCollider.gameObject);
        // }
        
        if (otherCollider.gameObject.layer == LayerMask.NameToLayer("BombLightingArea") &&
            m_playerData.m_bombsCarried > 0 && m_heldBombGameObject.GetComponent<BombController>().m_isLit == false)
        {
            m_heldBombGameObject.GetComponent<BombController>().SetLit(true);
        }
    }
    
    void OnTriggerExit(Collider otherCollider)
    {
        // if (otherCollider.gameObject.layer == LayerMask.NameToLayer("GoldDropZone"))
        // {
        //     if (m_inGoldDropZone)
        //     {
        //         int boatTeamNum = otherCollider.gameObject.GetComponent<GoldDropZoneData>().m_boat
        //             .GetComponent<BoatData>()
        //             .m_teamNum;
        //         if (boatTeamNum == m_playerData.m_teamNum)
        //         {
        //             m_inFriendlyGoldDropZone = false;
        //         }
        //         else
        //         {
        //             m_inEnemyGoldDropZone = false;
        //         }
        //         
        //         m_currentGoldDropzone = null;
        //         if (m_onPlayerExitGoldDropZone != null && m_onPlayerExitGoldDropZone.GetInvocationList().Length > 0)
        //         {
        //             m_onPlayerExitGoldDropZone(m_playerData.m_teamNum, m_playerData.m_playerNum);
        //         }
        //     }
        // }
        //
        // if (otherCollider.gameObject.layer == LayerMask.NameToLayer("GoldPickupZone"))
        // {
        //     if (m_inGoldPickupZone)
        //     {
        //         m_inGoldPickupZone = false;
        //         if (m_onPlayerExitGoldPickupZone != null && m_onPlayerExitGoldPickupZone.GetInvocationList().Length > 0)
        //         {
        //             m_onPlayerExitGoldPickupZone(m_playerData.m_teamNum, m_playerData.m_playerNum);
        //         }
        //     }
        // }
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
