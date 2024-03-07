using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldScoringParticlesController : MonoBehaviour
{
    [SerializeField] private float m_timeToGetToDestinationSeconds;

    [SerializeField] private float m_distanceFromCamera = 10f;

    [SerializeField] private float m_floatInPlaceAtStartTimeSeconds = 0f;

    [SerializeField] private GameObject m_particlesGameObject;

    [SerializeField] private Camera m_camera;

    private Vector2 m_screenStartingCoordinates;
    private Vector2 m_screenDestinationCoordinates;
    
    
    public void StartGoldScoringParticles(
        Vector2 screenStartingCoordinates,
        Vector2 screenDestinationCoordinates,
        float floatInPlaceAtStartTimeSeconds = -1f,
        float animationTimeSeconds = -1,
        Camera camera = null
        )
    {
        if (animationTimeSeconds != -1)
        {
            m_timeToGetToDestinationSeconds = animationTimeSeconds;
        }

        if (floatInPlaceAtStartTimeSeconds != -1)
        {
            m_floatInPlaceAtStartTimeSeconds = floatInPlaceAtStartTimeSeconds;
        }

        if (camera != null)
        {
            m_camera = camera;
        }
        else if (m_camera == null)
        {
            m_camera = Camera.main;
        }

        m_screenStartingCoordinates = screenStartingCoordinates;
        m_screenDestinationCoordinates = screenDestinationCoordinates;

        StartCoroutine(AnimateFromSourceToDestination());
    }

    private IEnumerator AnimateFromSourceToDestination()
    {
        m_particlesGameObject.SetActive(true);
        
        m_particlesGameObject.transform.position =
            m_camera.ScreenToWorldPoint(new Vector3(m_screenStartingCoordinates.x, m_screenStartingCoordinates.y, m_distanceFromCamera));

        yield return new WaitForSeconds(m_floatInPlaceAtStartTimeSeconds);
        
        for (float t = 0; t < 1; t += Time.deltaTime / m_timeToGetToDestinationSeconds)
        {
            Vector2 currentScreenPos = Vector2.Lerp(m_screenStartingCoordinates, m_screenDestinationCoordinates, t);

            m_particlesGameObject.transform.position =
                m_camera.ScreenToWorldPoint(new Vector3(currentScreenPos.x, currentScreenPos.y, m_distanceFromCamera));
            yield return null;
        }
        
        m_particlesGameObject.SetActive(false);
        
        Destroy(this.gameObject);
    }
}
