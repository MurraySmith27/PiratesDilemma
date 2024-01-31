using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(PlayerInput), typeof(PlayerData))]
public class PlayerGoldController : MonoBehaviour
{
    
    [SerializeField] public int m_goldCapacity = 3;

    [SerializeField] float m_goldPrefabScaleIncreaseFactor = 1.1f;
    
    [SerializeField] private AudioSource m_goldPickupAudioSource;

    [SerializeField] private GameObject m_heldGoldPrefab;

    private GameObject m_heldGoldInstance;
    
    private PlayerInput m_playerInput;
    
    private InputAction m_interactAction;

    private PlayerData m_playerData;

    private bool m_inGoldDropZone = false;
    private bool m_inGoldPickupZone = false;
    

    public int m_goldCarried;


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
        m_interactAction.performed += ctx => { OnPickupGold(ctx); };
        m_interactAction.performed += ctx => { OnDropGold(ctx); };
    }
    

    public void OnPickupGold(InputAction.CallbackContext ctx)
    {
        if (m_goldCarried < m_goldCapacity && m_inGoldPickupZone)
        {
            m_goldPickupAudioSource.Play();
            m_goldCarried += 1;

            if (m_heldGoldInstance == null)
            {
                SpawnGoldAsChild();
            }
            else
            {
                m_heldGoldInstance.transform.localScale *= m_goldPrefabScaleIncreaseFactor;
            }
        }
    }
    public void OnDropGold(InputAction.CallbackContext ctx)
    {
        if (m_goldCarried != 0 && m_inGoldDropZone)
        {
            GameObject boat = MaybeFindNearestBoat();
            
            if (boat && boat.GetComponent<BoatGoldController>().m_acceptingGold)
            {
                boat.GetComponent<BoatGoldController>().AddGold(m_goldCarried, GetComponent<PlayerData>().m_teamNum);
                DropAllGold();
                
            }
        }
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
    
    void SpawnGoldAsChild()
    {
        // Instantiate objectToSpawn as a child of this.transform
        m_heldGoldInstance = Instantiate(m_heldGoldPrefab, this.transform.position + new Vector3(1, 0, 0), this.transform.rotation, this.transform);

        m_heldGoldInstance.transform.localPosition = Vector3.forward * 2;
        m_heldGoldInstance.transform.localRotation = Quaternion.identity;
    }

    public void DropAllGold()
    {
        Destroy(m_heldGoldInstance);
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
