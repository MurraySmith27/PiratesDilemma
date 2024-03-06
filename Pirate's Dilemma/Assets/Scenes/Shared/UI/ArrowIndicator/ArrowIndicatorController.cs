using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowIndicatorController : MonoBehaviour
{
    [SerializeField] private float m_distanceFromCamera;
    
    
    [SerializeField] private float m_heightAboveToHover = 1f;
    
    private GameObject m_objectToTrack;
    
    private GameObject m_objectToPointTo;

    private float m_horizontalOffset = 0f;
    
    private float m_scaleFactor = 1f;

    private Coroutine m_hoverCoroutine;

    private Camera m_camera;
    private Color m_color;

    private Coroutine m_arrowTrackingCoroutine;

    public void StartArrowIndicatorController(float heightAboveToHover, 
        Color color,
        float horizontalOffset = 0f,
        float scaleFactor = -1f,
        GameObject objectToTrack = null,
        GameObject objectToPointTo = null,
        Camera camera = null)
    {
        if (objectToPointTo != null)
        {
            m_objectToPointTo = objectToPointTo;
        }
        
        if (objectToTrack != null)
        {
            m_objectToTrack = objectToTrack;
        }

        if (camera != null)
        {
            m_camera = camera ;
        }

        if (scaleFactor != -1f)
        {
            m_scaleFactor = scaleFactor;
        }

        m_color = color;
        
        m_horizontalOffset = horizontalOffset;

        m_heightAboveToHover = heightAboveToHover;

        m_arrowTrackingCoroutine = StartCoroutine(StartArrowTracking());
    }

    private IEnumerator StartArrowTracking()
    {
        MeshRenderer meshRenderer = GetComponentInChildren<MeshRenderer>();

        meshRenderer.enabled = true;

        Vector3 originalLocalScale = transform.localScale;
        
        while (true)
        {
            //first get direction from object to destination
            Vector3 lookDirection = m_objectToPointTo.transform.position - m_objectToTrack.transform.position;

            lookDirection = new Vector3(lookDirection.x, 0, lookDirection.z);

            transform.LookAt(transform.position + lookDirection, Vector3.up);
            
            //rotation set, now find position. Project up in camera space
            float zDistanceFromTrackingObject =
                Vector3.Distance(m_objectToTrack.transform.position, m_camera.transform.position) - 15f;
            
            Vector2 screenPos = m_camera.WorldToScreenPoint(m_objectToTrack.transform.position);

            Vector3 position1UnitAway = m_camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 1f));
            
            Vector3 screenPosModified = m_camera.WorldToScreenPoint(position1UnitAway + m_camera.transform.up * m_heightAboveToHover + m_camera.transform.right * m_horizontalOffset);
            
            transform.position = m_camera.ScreenToWorldPoint(new Vector3(screenPosModified.x, screenPosModified.y, zDistanceFromTrackingObject));
            
            //finally set our scale
            transform.localScale = originalLocalScale * (m_scaleFactor * zDistanceFromTrackingObject);

            yield return null;
        }
    }

    public void StopArrowIndicatorController()
    {
        StopCoroutine(m_arrowTrackingCoroutine);
        GetComponentInChildren<MeshRenderer>().enabled = false;
    }
    
}
