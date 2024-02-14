using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowTargetController : MonoBehaviour
{
    private CharacterController m_characterController;

    void Awake()
    {
        m_characterController = GetComponent<CharacterController>();
    }
    
    void LateUpdate()
    {
        float distanceToMoveDown = 9.81f * Time.deltaTime;
        RaycastHit hit;
        if (Physics.Raycast(
                transform.position - new Vector3(0,
                    m_characterController.center.y + m_characterController.height / 2, 0), Vector3.down,
                out hit, maxDistance: 0f, layerMask: ~LayerMask.GetMask("Floor")))
        {
            distanceToMoveDown = hit.distance;
        }

        m_characterController.Move(new Vector3(0, -distanceToMoveDown, 0));
    }
}
