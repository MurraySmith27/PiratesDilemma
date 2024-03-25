using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LevelEndScreenUIController : MonoBehaviour
{

    [SerializeField] private LevelEndSceneController m_levelEndSceneController;

    [SerializeField] private List<RenderTexture> m_loserSlotRenderTextures;

    private UIDocument m_doc;

    private VisualElement m_root;

    private VisualElement m_losersRoot;

    private List<VisualElement> m_loserFrames;
    
    private Label m_winnerText;

    private Coroutine m_renderTextureCoroutine;


    void Start()
    {
        m_doc = GetComponent<UIDocument>();

        m_root = m_doc.rootVisualElement.Q<VisualElement>("root");

        m_losersRoot = m_root.Q<VisualElement>("losers-root");

        m_winnerText = m_root.Q<Label>("winner-text");

        m_loserFrames = new List<VisualElement>();
        m_loserFrames.Add(m_root.Q<VisualElement>("loser-pane-1"));
        m_loserFrames.Add(m_root.Q<VisualElement>("loser-pane-2"));

        m_winnerText.text = PlayerSystem.Instance.m_teamData[m_levelEndSceneController.m_winningTeamNum-1].name + " Team!";

        m_levelEndSceneController.m_onImpactBackgroundAppear += OnImpactBackgroundAppear;
        
        m_levelEndSceneController.m_onImpactBackgroundDisappear += OnImpactBackgroundDisappear;
    }

    private void OnDestroy()
    {
        m_levelEndSceneController.m_onImpactBackgroundAppear -= OnImpactBackgroundAppear;
        
        m_levelEndSceneController.m_onImpactBackgroundDisappear -= OnImpactBackgroundDisappear;
        
        if (m_renderTextureCoroutine != null)
        {
            StopCoroutine(m_renderTextureCoroutine);
        }
    }

    private void OnImpactBackgroundAppear()
    {
        m_winnerText.ClearClassList();
    }


    private void OnImpactBackgroundDisappear()
    {
        m_losersRoot.ClearClassList();
        m_renderTextureCoroutine = StartCoroutine(ProjectRenderTexturesOntoLoserSlots());
    }

    private IEnumerator ProjectRenderTexturesOntoLoserSlots()
    {
        while (true)
        {

            // m_loserFrames[0].style.backgroundImage = m_loserSlotRenderTextures.;
            yield return null;
        }
    }


}
