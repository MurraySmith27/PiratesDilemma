using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ClosingCircleSpawner : MonoBehaviour
{
    private static ClosingCircleSpawner _instance;

    public static ClosingCircleSpawner Instance
    {
        get { return _instance; }
    }

    private VisualElement m_root;

    private Camera m_camera;
    
    private float m_closingSeconds = 0.5f;

    private float m_width = 10f;

    private float m_initialSize = 1000f;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    void Start()
    {
        m_root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("root");
        m_camera = Camera.main;
    }
    
    public void CreateClosingCircle(
        GameObject objectToTrack,
        Color tint,
        Camera camera = null,
        float closingSpeed = -1,
        float width = -1,
        float initialSize = -1
        )
    {

        if (camera != null)
        {
            m_camera = camera;
        }
        
        if (closingSpeed != -1)
        {
            m_closingSeconds = closingSpeed;
        }

        if (width != -1)
        {
            m_width = width;
        }
        
        if (initialSize != -1)
        {
            m_initialSize = initialSize;
        }
        
        
        CircleElement circleElement = new CircleElement();
        
        m_root.Add(circleElement);

        circleElement.m_width = m_width;
        circleElement.m_currentOuterRadius = m_initialSize;
        circleElement.m_tint = tint;

        StartCoroutine(AnimateCircleClosing(objectToTrack.transform, circleElement));
    }

    IEnumerator AnimateCircleClosing(Transform objectToTrackTransform, CircleElement circleElement)
    {
        float initialSize = m_initialSize;
        float finalSize = 0;

        for (float t = 0; t < 1; t += Time.deltaTime / m_closingSeconds)
        {
            float currentSize = finalSize * t + initialSize * (1 - t);

            circleElement.m_currentOuterRadius = currentSize;

            Vector2 position = m_camera.WorldToScreenPoint(objectToTrackTransform.position);

            circleElement.style.position = Position.Absolute;
            circleElement.style.left = position.x;
            circleElement.style.top = Screen.height - position.y;

            circleElement.MarkDirtyRepaint();
            
            yield return null;
        }

        m_root.Remove(circleElement);
    }
}
