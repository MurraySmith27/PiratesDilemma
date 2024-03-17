using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionController : MonoBehaviour
{

    [HideInInspector] public float m_explosionAliveSeconds;
    
    [HideInInspector] public float m_explosionAnimationSeconds;

    [HideInInspector] public float m_teamNum;

    [HideInInspector] public int m_boatDamage;
    
    
    void Start()
    {
        StartCoroutine(DestroyAfterWaiting());
    }

    private IEnumerator DestroyAfterWaiting()
    {
        yield return new WaitForSeconds(m_explosionAliveSeconds);
        
        this.GetComponent<Collider>().enabled = false;
        GetComponent<MeshRenderer>().enabled = false;
        yield return new WaitForSeconds(m_explosionAnimationSeconds - m_explosionAliveSeconds);
        
        Destroy(this.gameObject);
    }

}
