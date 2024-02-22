using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class GenericIndicatorController : MonoBehaviour
{
    [SerializeField] private Sprite m_hoverIcon;

    [SerializeField] private float m_heightAboveToHover = 1f;
    
    public GameObject m_objectToTrack;

    public float m_timeToLive = -1f;

    private Coroutine m_hoverCoroutine;

    private Camera m_camera;
    private Color m_color;

    public void StartIndicator(float heightAboveToHover, 
        Color color,
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

        if (scaleFactor > 0)
        {
            transform.localScale *= scaleFactor;
        }

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

        icon.style.unityBackgroundImageTintColor = new StyleColor(m_color);
        
        root.Q<VisualElement>("triangle").style.unityBackgroundImageTintColor = new StyleColor(m_color);
        
        for (float t = 0; t < 1; t += Time.deltaTime / m_timeToLive)
        {
            if (m_timeToLive < 0)
            {
                t = 0;
            }
            
            transform.position = m_objectToTrack.transform.position + new Vector3(0, m_heightAboveToHover, 0);
            
            transform.LookAt(transform.position + m_camera.transform.forward, Vector3.down);
            // if (m_camera.orthographic)
            // {
            //     //image is upside down, so down needs to be up
            //     transform.LookAt(transform.position + m_camera.transform.forward, Vector3.down);
            // }
            // else
            // {
            //     //image is upside down, so down needs to be up
            //     transform.LookAt(-m_camera.transform.position, Vector3.down);
            // }
            
            
            yield return null;
        }
        
        Destroy(this.gameObject);
    }
    
    
}
