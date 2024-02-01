using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void LooseGoldCollisionEvent();
public class LooseGoldController : MonoBehaviour
{
    public LooseGoldCollisionEvent m_onLooseGoldCollision;


    void OnCollisionEnter(Collision collision)
    {
        if ((collision.gameObject.layer & ~LayerMask.NameToLayer("Player")) != 0)
        {
            m_onLooseGoldCollision();
        }
    }
}
