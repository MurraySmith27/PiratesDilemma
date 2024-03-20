using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CircleWipeController : UIBase
{

    [SerializeField] private float m_transitionTime = 0.5f;
    
    [SerializeField] private bool m_startCovered = false;

    [SerializeField] private AnimationCurve m_animCurve;

    [SerializeField] private Color m_circleTint;
    
    private VisualElement m_root;
    private VisualElement m_circle;

    
    protected override void Awake()
    {
        base.Awake();
    }
    
    protected override void SetUpUI()
    {
        m_root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("root");

        m_circle = m_root.Q<VisualElement>("circle");

        GameTimerSystem.Instance.m_onGameSceneUnloaded += StartTransitionOut;

        GameTimerSystem.Instance.m_onGameSceneLoaded += StartTransitionIn;

        if (m_startCovered)
        {
            m_circle.style.width = Length.Percent(158);
            m_circle.style.height = Length.Percent(158);
        }

        m_circle.style.unityBackgroundImageTintColor = m_circleTint;
    }

    void OnDestroy()
    {
        GameTimerSystem.Instance.m_onGameSceneUnloaded -= StartTransitionOut;

        GameTimerSystem.Instance.m_onGameSceneLoaded -= StartTransitionIn;
    }


    private void StartTransitionOut()
    {
        StartCoroutine(TransitionOutAnimation());
    }

    private IEnumerator TransitionOutAnimation()
    {
        float circleInitialPercent = 0f;
        var circleFinalPercent = 158f;
        for (float t = 0f; t < 1f; t += Time.deltaTime / m_transitionTime)
        {
            float currentPercent = (1f - t) * circleInitialPercent + t * circleFinalPercent;

            m_circle.style.width = Length.Percent(currentPercent);
            m_circle.style.height = Length.Percent(currentPercent);

            yield return null;
        }
    }

    private void StartTransitionIn()
    {
        StartCoroutine(TransitionInAnimation());
    }

    private IEnumerator TransitionInAnimation()
    {
        float circleInitialPercent = 158f;
        float circleFinalPercent = 0f;
        
        for (float t = 0; t < 1f; t += Time.deltaTime / m_transitionTime)
        {
            float currentPercent = ((1f - t) * circleInitialPercent + t * circleFinalPercent);
            
            
            m_circle.style.width = Length.Percent(currentPercent);
            m_circle.style.height = Length.Percent(currentPercent);

            yield return null;
        }
    }
    
}
