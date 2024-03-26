using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class LevelEndScreenUIController : MonoBehaviour
{

    [SerializeField] private LevelEndSceneController m_levelEndSceneController;

    [SerializeField] private List<RenderTexture> m_loserSlotRenderTextures;

    [SerializeField]
    private Renderer background;

    private VisualElement m_menuRoot;
    
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

        m_menuRoot = m_root.Q<VisualElement>("menu-root");

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

        PlayerInput playerInput = PlayerSystem.Instance.m_players[0].GetComponent<PlayerInput>();

        playerInput.actions.FindAction("Select").performed += OnSelectPressedFirstTime;
    }

    private void OnSelectPressedFirstTime(InputAction.CallbackContext ctx)
    {
        m_menuRoot.ClearClassList();
    }

    private IEnumerator ProjectRenderTexturesOntoLoserSlots()
    {
        while (true)
        {
            for (int i = 0; i < 2; i++)
            {
                RenderTexture.active = m_loserSlotRenderTextures[i];
                Texture2D tex = new Texture2D(m_loserSlotRenderTextures[i].width, m_loserSlotRenderTextures[i].height);
                tex.ReadPixels(new Rect(0, 0, m_loserSlotRenderTextures[i].width, m_loserSlotRenderTextures[i].height),
                    0, 0);
                tex.Apply();
                m_loserFrames[i].style.backgroundImage = tex;

                List<Material> mats = new();

                background.GetMaterials(mats);

                mats[0].SetTexture("_Base_Map", tex);
            }

            yield return null;
        }
    }


}
