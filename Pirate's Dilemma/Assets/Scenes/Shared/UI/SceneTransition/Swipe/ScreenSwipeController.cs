using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ScreenSwipeController : UIBase
{

    [SerializeField] private float m_transitionTime;
        
    private VisualElement m_root;
    private VisualElement m_swipeInScreen;

    protected override void SetUpUI()
    {
        m_root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("root");

        m_swipeInScreen = m_root.Q<VisualElement>("swipe-cover");
    }
    
    
}
