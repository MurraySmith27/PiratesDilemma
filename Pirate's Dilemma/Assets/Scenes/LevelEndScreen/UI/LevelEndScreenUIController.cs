using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class LevelEndScreenUIController : MonoBehaviour
{

    [SerializeField] private LevelEndSceneController m_levelEndSceneController;

    [SerializeField] private List<RenderTexture> m_loserSlotRenderTextures;

    [SerializeField] private StudioEventEmitter m_menuClickEventEmitter;

    [SerializeField]
    private Renderer background;

    private VisualElement m_menuRoot;
    
    private UIDocument m_doc;

    private VisualElement m_root;

    private VisualElement m_mainRoot;

    private VisualElement m_losersRoot;

    private List<VisualElement> m_loserFrames;

    private List<Button> m_menuItems;
    
    private Label m_winnerText;

    private Coroutine m_renderTextureCoroutine;

    private int m_currentlySelectedMenuItemIndex;

    void Start()
    {
        m_doc = GetComponent<UIDocument>();

        m_root = m_doc.rootVisualElement.Q<VisualElement>("root");
        
        m_mainRoot = m_root.Q<VisualElement>("main-root");

        m_losersRoot = m_root.Q<VisualElement>("losers-root");

        m_winnerText = m_root.Q<Label>("winner-text");

        m_loserFrames = new List<VisualElement>();
        m_loserFrames.Add(m_root.Q<VisualElement>("loser-pane-1"));
        m_loserFrames.Add(m_root.Q<VisualElement>("loser-pane-2"));

        m_menuRoot = m_root.Q<VisualElement>("menu-root");
        
        m_menuItems = new List<Button>();

        Button nextLevelButton = m_menuRoot.Q<Button>("next-level-button");
        if (GameTimerSystem.Instance.GetNextLevelName() != "")
        {
            nextLevelButton.clicked += () => LoadNextScene(GameTimerSystem.Instance.GetNextLevelName());
            m_menuItems.Add(nextLevelButton);
        }
        else
        {
            nextLevelButton.AddToClassList("menu-button-disabled");    
        }

        Button levelSelectButton = m_menuRoot.Q<Button>("level-select-button"); 
        levelSelectButton.clicked += () => LoadNextScene(GameTimerSystem.Instance.m_levelSelectSceneName);
        m_menuItems.Add(levelSelectButton);

        Button characterSelectButton = m_menuRoot.Q<Button>("character-select-button");
        characterSelectButton.clicked += () => LoadNextScene(GameTimerSystem.Instance.m_characterSelectSceneName);
        m_menuItems.Add(characterSelectButton);

        m_winnerText.text = PlayerSystem.Instance.m_teamData[m_levelEndSceneController.m_winningTeamNum-1].name + " Team!";

        m_levelEndSceneController.m_onImpactBackgroundAppear += OnImpactBackgroundAppear;
        
        m_levelEndSceneController.m_onImpactBackgroundDisappear += OnImpactBackgroundDisappear;

    }

    private void LoadNextScene(string nextSceneName)
    {
        
        Debug.Log($"button on click occured!! next scene: {nextSceneName}");
        GameTimerSystem.Instance.StopGame(nextSceneName);
    }
    
    private void OnDestroy()
    {
        m_levelEndSceneController.m_onImpactBackgroundAppear -= OnImpactBackgroundAppear;
        
        m_levelEndSceneController.m_onImpactBackgroundDisappear -= OnImpactBackgroundDisappear;
        
        
        PlayerInput playerInput = PlayerSystem.Instance.m_players[0].GetComponent<PlayerInput>();
        playerInput.actions.FindAction("Select").performed -= OnSelectPressedFirstTime;
        playerInput.actions.FindAction("Select").Disable();
        
        playerInput.actions.FindAction("NavigateUI").performed -= OnNavigateUI;
        playerInput.actions.FindAction("NavigateUI").Disable();
        
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

        PlayerInput playerInput = PlayerSystem.Instance.m_players[0].GetComponent<PlayerInput>();

        playerInput.actions.FindAction("Select").performed += OnSelectPressedFirstTime;
        playerInput.actions.FindAction("Select").Enable();
    }

    private void OnSelectPressedFirstTime(InputAction.CallbackContext ctx)
    {
        m_menuRoot.ClearClassList();
        m_menuRoot.AddToClassList("menu-root-style");
        
        m_mainRoot.AddToClassList("losers-root-up");

        PlayerInput playerInput = PlayerSystem.Instance.m_players[0].GetComponent<PlayerInput>();

        playerInput.actions.FindAction("NavigateUI").performed += OnNavigateUI;
        playerInput.actions.FindAction("NavigateUI").Enable();

        playerInput.actions.FindAction("Select").performed -= OnSelectPressedFirstTime;
        playerInput.actions.FindAction("Select").performed += OnButtonSelected;
        playerInput.actions.FindAction("Select").Enable();

        m_currentlySelectedMenuItemIndex = 0;

        UpdateMenuButtons();
    }

    private void OnButtonSelected(InputAction.CallbackContext ctx)
    {
        Button button = m_menuItems[m_currentlySelectedMenuItemIndex];
        //simulate a click event.
        using (var e = new NavigationSubmitEvent() { target = button })
        {
            button.SendEvent(e);
        }

    }

    private void OnNavigateUI(InputAction.CallbackContext ctx)
    {
        PlayerInput playerInput = PlayerSystem.Instance.m_players[0].GetComponent<PlayerInput>();

        Vector2 moveDirection = playerInput.actions.FindAction("NavigateUI").ReadValue<Vector2>();

        if (moveDirection.magnitude == 0)
        {
            return;
        }
        else if (Mathf.Abs(moveDirection.x) > Mathf.Abs(moveDirection.y))
        {
            if (moveDirection.x > 0)
            {
                //move right
                
            }
            else
            {
                //move left
            }
        }
        else
        {
            if (moveDirection.y > 0)
            {
                //move up
                if (m_currentlySelectedMenuItemIndex > 0)
                {
                    m_currentlySelectedMenuItemIndex--;
                    m_menuClickEventEmitter.Play();
                }
            }
            else
            {
                //move down
                if (m_currentlySelectedMenuItemIndex < m_menuItems.Count - 1)
                {
                    
                    m_currentlySelectedMenuItemIndex++;
                    m_menuClickEventEmitter.Play();
                }
            }

        }
        UpdateMenuButtons();
    }

    private void UpdateMenuButtons()
    {
        m_menuItems[m_currentlySelectedMenuItemIndex].Focus();
    }
}
