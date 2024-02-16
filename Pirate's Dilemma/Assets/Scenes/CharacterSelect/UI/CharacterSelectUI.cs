
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
    [SerializeField] private GameObject m_gameTestScreen;
    private List<UIDocument> m_docs;

    [SerializeField] private Sprite m_playstationControllerReadyUpIcon;
    [SerializeField] private Sprite m_xboxControllerReadyUpIcon;
    [SerializeField] private Sprite m_keyboardReadyUpIcon;

    [SerializeField] private Camera m_testControlsCamera;

    private List<bool> m_readyPlayers;

    private List<InputAction> m_readyUpActions;

    private Coroutine m_startGameCountdownCoroutine;
    
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
            
            m_renderTextureQuads[i].transform.localScale = new Vector3(quadWidthScale, quadHeightScale / 2f, 1);
            Vector3 currentPos = m_renderTextureQuads[i].transform.position;
            m_renderTextureQuads[i].transform.position = new Vector3(-quadWidthScale * ((i % 2) - 1.5f), currentPos.y - (2 * (int)(i/2)-1) * quadHeightScale / 4f, currentPos.z);
            
            m_docs.Add(m_renderTextureQuads[i].GetComponent<UIDocument>());
            
            m_pressToJoinElements.Add(m_docs[i].rootVisualElement);
            m_pressToJoinElements[i].Q<Label>("player-label").text = $"Player {i + 1}";
        }

        m_gameTestScreen.transform.localScale = new Vector3(quadWidthScale * 2f, quadHeightScale, 1);
        m_gameTestScreen.transform.position += new Vector3(-quadWidthScale, 0, 0);
        
        PlayerSystem.Instance.m_onPlayerJoin += OnPlayerJoin;
        PlayerSystem.Instance.m_onPlayerReadyUpToggle += UpdateReadyUpUI;
    }

    void OnPlayerJoin(int newPlayerNum)
    {
        m_pressToJoinElements[newPlayerNum - 1].Q<Label>("press-to-join-label").text = "";

        VisualElement newReadyUpHover = m_readyUpHover.Instantiate();
        m_readyUpHoverElements.Add(newReadyUpHover);
        
        m_root.Add(newReadyUpHover);
        
        Vector3 screen = Camera.main.WorldToScreenPoint(m_renderTextureQuads[newPlayerNum - 1].transform.position);
        newReadyUpHover.style.left =
            screen.x - 75;
        newReadyUpHover.style.top = (Screen.height - screen.y) + (Screen.height * 0.15f);

        PlayerSystem.Instance.SwitchToActionMapForPlayer(newPlayerNum, "CharacterSelect");

        InputDevice device = PlayerSystem.Instance.m_playerInputObjects[newPlayerNum - 1].devices[0];
        
        Image image = new Image();
        if (device is DualShockGamepad)
        {
            image.sprite = m_playstationControllerReadyUpIcon;
        }
        else if (device is Gamepad)
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
        
        genericIndicatorInstance.GetComponent<GenericIndicatorController>().StartIndicator(3f, 
            PlayerSystem.Instance.m_teamColors[teamAssignment - 1],
            hoverIcon: m_playerNumberIcons[newPlayerNum - 1], 
            objectToTrack: PlayerSystem.Instance.m_players[newPlayerNum - 1],
            scaleFactor: 2f,
            camera: m_testControlsCamera);

        for (int i = 0; i < PlayerSystem.Instance.m_playersParents[newPlayerNum - 1].transform.childCount; i++)
        {
            Transform child = PlayerSystem.Instance.m_playersParents[newPlayerNum - 1].transform.GetChild(i);
            if (child.gameObject.tag == "VisualStandIn")
            {
                GameObject visualStandInGenericIndicatorInstance = Instantiate(m_playerHoverPrefabs[newPlayerNum - 1],
                    new Vector3(0, 0, 0),
                    Quaternion.identity);

                visualStandInGenericIndicatorInstance.GetComponent<GenericIndicatorController>().StartIndicator(3f,
                    PlayerSystem.Instance.m_teamColors[teamAssignment - 1],
                    hoverIcon: m_playerNumberIcons[newPlayerNum - 1],
                    objectToTrack: child.gameObject,
                    camera: Camera.main);
            }
        }

        UpdateReadyUpUI(newPlayerNum, false);
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
}
