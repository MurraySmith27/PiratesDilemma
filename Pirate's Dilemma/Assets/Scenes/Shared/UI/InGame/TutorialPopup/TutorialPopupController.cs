using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TutorialPopupController : MonoBehaviour
{
    private VisualElement m_root;

    private VisualElement m_popupContainer;


    void Start()
    {
        m_root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("root");

        m_popupContainer = m_root.Q<VisualElement>("popup-container");
    }

    public void ShowPopup(Sprite popupImage)
    {
        Debug.Log("showing popup!");
        m_popupContainer.style.backgroundImage = popupImage.texture;
        m_popupContainer.style.visibility = Visibility.Visible;
    }

    public void HidePopup()
    {
        Debug.Log("hiding popup!");
        m_popupContainer.style.visibility = Visibility.Hidden;
    }
}
