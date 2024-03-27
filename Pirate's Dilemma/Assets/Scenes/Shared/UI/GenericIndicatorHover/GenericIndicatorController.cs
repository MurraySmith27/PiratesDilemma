using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Vector2 = UnityEngine.Vector2;

public class GenericIndicatorController : MonoBehaviour
{
    [SerializeField] private Sprite m_hoverIcon;

    [SerializeField] private float m_heightAboveToHover = 1f;
    
    public GameObject m_objectToTrack;

    public float m_timeToLive = -1f;

    private float m_horizontalOffset = 0f;
    
    private float m_scaleFactor = 1f;

    private Coroutine m_hoverCoroutine;

    private Camera m_camera;
    public Color m_color;

    public void StartIndicator(float heightAboveToHover, 
        Color color,
        float horizontalOffset = 0f,
        float scaleFactor = -1f,
        Sprite hoverIcon = null, 
        GameObject objectToTrack = null, 
        float timeToLive = -1f,
        Camera camera = null)
    {

        if (objectToTrack != null)
        {
            m_objectToTrack = objectToTrack;
        }


        if (timeToLive > 0)
        {
            m_timeToLive = timeToLive;
        }

        if (hoverIcon != null)
        {
            m_hoverIcon = hoverIcon;
        }

        if (camera != null)
        {
            m_camera = camera ;
        }

        if (scaleFactor != -1f)
        {
            m_scaleFactor = scaleFactor;
        }

        m_horizontalOffset = horizontalOffset; 

        m_color = color;

        m_heightAboveToHover = heightAboveToHover;

        m_hoverCoroutine = StartCoroutine(HoverTimer());;
    }

    private IEnumerator HoverTimer()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        meshRenderer.enabled = true;

        VisualElement root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("root");

        Image image = new Image();
        image.sprite = m_hoverIcon;

            
        VisualElement icon = root.Q<VisualElement>("generic-icon");
        icon.style.backgroundImage = new StyleBackground(image.sprite);

        Vector3 originalLocalScale = transform.localScale;
        
        for (float t = 0; t < 1; t += Time.deltaTime / m_timeToLive)
        {
            if (m_timeToLive < 0)
            {
                t = 0;
            }

            Vector2 screenPos = m_camera.WorldToScreenPoint(m_objectToTrack.transform.position);

            float zDistanceFromTrackingObject =
                Vector3.Distance(m_objectToTrack.transform.position, m_camera.transform.position) - 15f;

            Vector3 position1UnitAway = m_camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 1f));
            
            Vector3 screenPosModified = m_camera.WorldToScreenPoint(position1UnitAway + m_camera.transform.up * m_heightAboveToHover + m_camera.transform.right * m_horizontalOffset);
            
            transform.position = m_camera.ScreenToWorldPoint(new Vector3(screenPosModified.x, screenPosModified.y, zDistanceFromTrackingObject));

            transform.localScale = originalLocalScale * (m_scaleFactor * zDistanceFromTrackingObject);
            
            transform.LookAt(transform.position + m_camera.transform.forward, Vector3.up);
            
            icon.style.unityBackgroundImageTintColor = new StyleColor(m_color);
        
            root.Q<VisualElement>("triangle").style.unityBackgroundImageTintColor = new StyleColor(m_color);

            yield return null;
        }
        
        Destroy(this.gameObject);
    }
    
    
}
