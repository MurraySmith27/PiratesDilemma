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
        Debug.Log($"layer on creation: {gameObject.layer}");
    }

    IEnumerator DespawnAfterSeconds()
    {
        yield return new WaitForSeconds(5);
        Destroy(this.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log(
            $"collided with object: {collision.gameObject.name}, layer: {collision.gameObject.layer}. Current layer: {gameObject.layer}");
        if (collision.gameObject.layer != LayerMask.NameToLayer("Player"))
        {
            if (m_onLooseGoldCollision.GetInvocationList().Length > 0)
            {
                Debug.Log("stopping flight!");
                m_onLooseGoldCollision();
            }
        }
    }
}
