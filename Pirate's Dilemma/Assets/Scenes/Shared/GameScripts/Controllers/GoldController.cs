using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(PlayerInput), typeof(PlayerDataController))]
public class GoldController : MonoBehaviour
{
    
    private PlayerInput m_playerInput;
    private InputAction m_interactAction;
    
    private GameObject goldPile;
    private GameObject spawnedGold;

    public GameObject goldToSpawn;
    private bool goldPrefabSpawned = false;

    public float goldPrefabScaleIncreaseFactor = 1.1f;

    public int goldCarried = 0;

    public int goldCapacity = 30;

    private bool m_initialized = false;

    void Awake()
    {
        GameStartSystem.Instance.m_onGameStart += OnGameStart;
    }
    
    void OnGameStart()
    {   
        //Assigning Callbacks

        m_playerInput = GetComponent<PlayerInput>();

        m_interactAction = m_playerInput.actions["Interact"];
        m_interactAction.performed += ctx => { OnPickupGold(ctx); };
        m_interactAction.performed += ctx => { OnDropGold(ctx); };

        //Finding GameObjects
        if (goldPile == null)
        {
            goldPile = GameObject.FindGameObjectWithTag("GoldPile");
        }

        m_initialized = true;
    }
    

    public void OnPickupGold(InputAction.CallbackContext ctx)
    {
        if (goldCarried < goldCapacity)
        {
            //Check if close enough to gold pile
            if ((this.transform.position - goldPile.transform.position).magnitude < 50)
            {
                GetComponent<AudioSource>().Play();
                goldCarried += 5;

                if (!goldPrefabSpawned)
                {
                    SpawnGoldAsChild();
                }
                else
                {
                    spawnedGold.transform.localScale *= goldPrefabScaleIncreaseFactor;
                }

            }
        }
    }
    public void OnDropGold(InputAction.CallbackContext ctx)
    {
        if (goldCarried != 0)
        {
            GameObject boat = MaybeFindNearestBoat();
            
            if (boat && boat.GetComponent<BoatController>().acceptingGold)
            {
                Destroy(spawnedGold);
                goldPrefabSpawned = false;
                boat.GetComponent<BoatController>().AddGold(goldCarried, GetComponent<PlayerDataController>().m_playerNum);
                goldCarried = 0;
                
            }
        }
    }

    public GameObject MaybeFindNearestBoat()
    {
        GameObject nearestBoat = null;

        foreach (GameObject boat in GameObject.FindGameObjectsWithTag("Boat"))
        {
            if ((!nearestBoat && (this.transform.position - boat.transform.position).magnitude < 10) ||
                nearestBoat && 
                ((this.transform.position - boat.transform.position).magnitude < 
                 (this.transform.position - nearestBoat.transform.position).magnitude))
            {
                nearestBoat = boat;
            }
        }
        
        return nearestBoat;
    }
    
    void SpawnGoldAsChild()
    {
        goldPrefabSpawned = true;
        // Instantiate objectToSpawn as a child of this.transform
        spawnedGold = Instantiate(goldToSpawn, this.transform.position + new Vector3(1, 0, 0), this.transform.rotation, this.transform);

        // Optionally, set the local position and rotation of the spawned object relative to the parent
        spawnedGold.transform.localPosition = Vector3.forward * 2; // Set to zero if you want it to be at the parent's position
        spawnedGold.transform.localRotation = Quaternion.identity; // Set to identity if you want it to have the parent's rotation
    }
    
}
