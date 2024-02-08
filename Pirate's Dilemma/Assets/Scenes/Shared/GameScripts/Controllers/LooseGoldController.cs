using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void LooseGoldCollisionEvent();
public class LooseGoldController : MonoBehaviour
{
    public LooseGoldCollisionEvent m_onLooseGoldCollision;

    void Start()
    {
        StartCoroutine(DespawnAfterSeconds());
    }

    IEnumerator DespawnAfterSeconds()
    {
        yield return new WaitForSeconds(5);
        Destroy(this.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        
        Debug.Log($"on loose gold collision. Layer: {collision.gameObject.layer}");
        if ((collision.gameObject.layer & ~LayerMask.NameToLayer("Player")) != 0)
        {
            Debug.Log("layer mask");
            m_onLooseGoldCollision();
        }
    }
}
