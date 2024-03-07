using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ScreenSwipeController : UIBase
{

    [SerializeField] private float m_transitionTime = 0.5f;
    
    [SerializeField] private bool m_startCovered = false;
        
    private VisualElement m_root;
    private VisualElement m_swipeInScreen;

    
    protected override void Awake()
    {
        base.Awake();
    }
    
    protected override void SetUpUI()
    {
        m_root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("root");

        m_swipeInScreen = m_root.Q<VisualElement>("swipe-cover");

        GameTimerSystem.Instance.m_onGameSceneUnloaded += StartTransitionOut;

        GameTimerSystem.Instance.m_onGameSceneLoaded += StartTransitionIn;

        if (m_startCovered)
        {
            m_swipeInScreen.style.left = 0;
        }
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
        float swipeInitialLeft = Screen.width;
        float swipeFinalLeft = 0;
        
        for (float t = 0; t < 1; t += Time.deltaTime / m_transitionTime)
        {
            float padding = (1 - t) * swipeInitialLeft + t * swipeFinalLeft;

            m_swipeInScreen.style.left = padding;

            yield return null;
        }
    }

    private void StartTransitionIn()
    {
        StartCoroutine(TransitionInAnimation());
    }

    private IEnumerator TransitionInAnimation()
    {
        float swipeInitialLeft = 0;
        float swipeFinalLeft = -Screen.width;
        
        for (float t = 0; t < 1; t += Time.deltaTime / m_transitionTime)
        {
            float padding = (1 - t) * swipeInitialLeft + t * swipeFinalLeft;

            m_swipeInScreen.style.left = padding;

            yield return null;
        }
    }
    
}
