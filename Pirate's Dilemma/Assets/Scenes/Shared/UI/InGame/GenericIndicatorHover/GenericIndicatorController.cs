using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GenericIndicatorController : MonoBehaviour
{
    [SerializeField] private float m_verticalStretchAnimationAmount = 1f;

    [SerializeField] private float m_horizontalStretchAnimationAmount = 1f;

    [SerializeField] private float m_animationSpeed;
    private float t = 0f;
    
    // Update is called once per frame
    void Update()
    {

        t += Time.deltaTime;

        float verticalStretch = Mathf.Sin(t * m_animationSpeed) * m_verticalStretchAnimationAmount;
    }
}
