using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatSwayController : MonoBehaviour
{

    [SerializeField] private float m_swaySpeed = 1f;
    [SerializeField] private float m_swayDegrees = 20f;
    [SerializeField] private float m_verticalBobPeriod = 2f;
    [SerializeField] private float m_verticalBobAmount = 0.2f;
    void Start()
    {
        StartCoroutine(Sway());
    }

    private IEnumerator Sway()
    {

        float randomOffset = Random.Range(0, 1f);
        
        Vector3 initialPosition = transform.localPosition;
        
        bool forward = true;

        bool first = true;
        while (true)
        {
            
            //sway left
            Quaternion initialRotation = transform.rotation;

            float degrees = -m_swayDegrees;
            if (forward)
            {
                degrees = m_swayDegrees;
            }

            if (first)
            {
                degrees = degrees / 2;
            }
            
            Quaternion finalRotation = Quaternion.RotateTowards(initialRotation, Quaternion.Euler(degrees, 0, 0), Mathf.Abs(degrees));

            for (float t = 0; t < 1; t += Time.deltaTime / m_swaySpeed)
            {
                if (first)
                {
                    t = randomOffset;
                    first = false;
                }
                transform.localPosition = initialPosition + new Vector3(0, 0.2f * Mathf.Sin(2 * Mathf.PI * t / m_verticalBobPeriod), 0);
                
                transform.rotation = Quaternion.Lerp(initialRotation, finalRotation, t);
                yield return null;
            }

            forward = !forward;
        }
    }
    
}