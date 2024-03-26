
using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.SceneManagement;

public class CharacterSelectUI : UIBase
{

    [SerializeField] private UIDocument m_screenSpaceUIDoc;
    private VisualElement m_root;

    [SerializeField] private VisualTreeAsset m_readyUpHover;

    [SerializeField] private List<GameObject> m_playerHoverPrefabs;

    [SerializeField] private List<Sprite> m_playerNumberIcons;

    private List<VisualElement> m_pressToJoinElements;
    private List<VisualElement> m_readyUpHoverElements;

    [SerializeField] private List<GameObject> m_renderTextureQuads;
    private List<UIDocument> m_docs;

    [SerializeField] private Sprite m_playstationControllerReadyUpIcon;
    [SerializeField] private Sprite m_xboxControllerReadyUpIcon;
    [SerializeField] private Sprite m_keyboardReadyUpIcon;

    [SerializeField] private Camera m_testControlsCamera;

    private List<bool> m_readyPlayers;

    private List<InputAction> m_readyUpActions;

    private List<SupportedDeviceType> m_deviceTypesPerPlayer;

    private Coroutine m_startGameCountdownCoroutine;
    
    private Coroutine m_updateReadyUpHoverCoroutine;

    private enum SupportedDeviceType
    {
        DualshockGamepad,
        Gamepad,
        Keyboard
    }
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void SetUpUI()
    {
        m_root = m_screenSpaceUIDoc.rootVisualElement.Q<VisualElement>("root");
        m_docs = new List<UIDocument>();

        m_pressToJoinElements = new List<VisualElement>();
        m_readyUpHoverElements = new List<VisualElement>();
        m_readyUpActions = new List<InputAction>();
        m_readyPlayers = new List<bool>();
        m_deviceTypesPerPlayer = new List<SupportedDeviceType>();
        
        //resize the render texture quads to fit the screen properly
        Vector3 cameraPos = Camera.main.transform.position;
        float quadToCameraDistance =
            Vector3.Distance(m_renderTextureQuads[0].transform.position, cameraPos);
        // uncomment for perspective camera:
        // float quadHeightScale = 2f * Mathf.Tan(0.5f * Camera.main.fieldOfView * Mathf.Deg2Rad) *
        //                         quadToCameraDistance; 
        float quadHeightScale = Camera.main.orthographicSize * 2;
        float quadWidthScale = quadHeightScale * Camera.main.aspect / PlayerSystem.Instance.m_maxNumPlayers;
        
        for (int i = 0; i < PlayerSystem.Instance.m_maxNumPlayers; i++)
        {
            // m_renderTextureQuads[i].transform.localScale = new Vector3(quadWidthScale, quadHeightScale / 2f, 1)p;
            Vector3 currentPos = m_renderTextureQuads[i].transform.position;
            m_renderTextureQuads[i].transform.position =
                GameObject.FindGameObjectWithTag($"P{i + 1}CharacterSelectSpawn").transform.position;
            
            m_renderTextureQuads[i].transform.LookAt(transform.position -Camera.main.transform.position);
            
            m_docs.Add(m_renderTextureQuads[i].GetComponent<UIDocument>());

            m_deviceTypesPerPlayer.Add(SupportedDeviceType.Keyboard);
            
            m_pressToJoinElements.Add(m_docs[i].rootVisualElement);
            m_pressToJoinElements[i].Q<Label>("player-label").text = $"Player {i + 1}";
        }
        
        PlayerSystem.Instance.m_onPlayerJoin += OnPlayerJoin;
        PlayerSystem.Instance.m_onPlayerReadyUpToggle += UpdateReadyUpUI;

        GameTimerSystem.Instance.m_onCharacterSelectEnd += OnCharacterSelectEnd;
    }

    void OnPlayerJoin(int newPlayerNum)
    {
        m_pressToJoinElements[newPlayerNum - 1].Q<Label>("press-to-join-label").text = "";

        VisualElement newReadyUpHover = m_readyUpHover.Instantiate();
        m_readyUpHoverElements.Add(newReadyUpHover);

        newReadyUpHover.Q<Label>("ready-text").text = "Ready";
        m_root.Add(newReadyUpHover);

        PlayerSystem.Instance.SwitchToActionMapForPlayer(newPlayerNum, "CharacterSelect");

        if (PlayerSystem.Instance.m_playerInputObjects[newPlayerNum - 1].devices.Count > 0)
        {
            InputDevice device = PlayerSystem.Instance.m_playerInputObjects[newPlayerNum - 1].devices[0];
            if (device is DualShockGamepad)
            {
                m_deviceTypesPerPlayer[newPlayerNum - 1] = SupportedDeviceType.DualshockGamepad;
            }
            else if (device is Gamepad)
            {
                m_deviceTypesPerPlayer[newPlayerNum - 1] = SupportedDeviceType.Gamepad;
            }
            else
            {
                m_deviceTypesPerPlayer[newPlayerNum - 1] = SupportedDeviceType.Keyboard;
            }
        }
        
        Image image = new Image();
        if (m_deviceTypesPerPlayer[newPlayerNum-1] == SupportedDeviceType.DualshockGamepad)
        {
            image.sprite = m_playstationControllerReadyUpIcon;
        }
        else if (m_deviceTypesPerPlayer[newPlayerNum-1] == SupportedDeviceType.Gamepad)
        {
            image.sprite = m_xboxControllerReadyUpIcon;
        }
        else
        {
            image.sprite = m_keyboardReadyUpIcon;
        }
        
        newReadyUpHover.Q<VisualElement>("button-icon").Add(image);

        GameObject genericIndicatorInstance = Instantiate(m_playerHoverPrefabs[newPlayerNum - 1], new Vector3(0, 0, 0),
            Quaternion.identity);

        int teamAssignment = PlayerSystem.Instance.m_playerTeamAssignments[newPlayerNum - 1];
        
        genericIndicatorInstance.GetComponent<GenericIndicatorController>().StartIndicator(0.1f, 
            PlayerSystem.Instance.m_teamData[teamAssignment - 1].color,
            hoverIcon: m_playerNumberIcons[newPlayerNum - 1], 
            objectToTrack: PlayerSystem.Instance.m_players[newPlayerNum - 1],
            scaleFactor: 0.1f,
            camera: m_testControlsCamera);

        UpdateReadyUpUI(newPlayerNum, false);
        
        m_updateReadyUpHoverCoroutine = StartCoroutine(UpdateReadyUpHoverPositions());
    }

    private IEnumerator UpdateReadyUpHoverPositions()
    {
        while(true) {
            for (int i = 0; i < PlayerSystem.Instance.m_numPlayers; i++)
            {
                Vector3 screen = Camera.main.WorldToScreenPoint(PlayerSystem.Instance.m_visualStandIns[i].transform.position);
                m_readyUpHoverElements[i].style.left =
                    screen.x - 75;
                m_readyUpHoverElements[i].style.top = (Screen.height - screen.y);
            }

            yield return null;
        }
    }

    private void OnCharacterSelectEnd()
    {
        StopCoroutine(m_updateReadyUpHoverCoroutine);
        foreach (VisualElement elem in m_readyUpHoverElements)
        {
            elem.style.visibility = Visibility.Hidden;
        }
    }

    private void UpdateReadyUpUI(int playerNum, bool isReady)
    {
        Color color;
        if (isReady)
        {
            color = Color.green;
        }
        else
        {
            color = Color.red;
        }
        m_readyUpHoverElements[playerNum - 1].Q<VisualElement>("root").style.backgroundColor = new StyleColor(color);
    }

    void OnDisable()
    {
        PlayerSystem.Instance.m_onPlayerJoin -= OnPlayerJoin;
        PlayerSystem.Instance.m_onPlayerReadyUpToggle -= UpdateReadyUpUI;
    }
}
