
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CharacterSelectUIController : MonoBehaviour
{

    [SerializeField] private UIDocument m_screenSpaceUIDoc;
    private VisualElement m_root;

    public string m_gameSceneToLoadName;

    [SerializeField] private VisualTreeAsset m_readyUpHover;

    private List<VisualElement> m_pressToJoinElements;
    private List<VisualElement> m_readyUpHoverElements;

    [SerializeField] private List<GameObject> m_renderTextureQuads;
    private List<UIDocument> m_docs;

    [SerializeField] private Sprite m_controllerReadyUpIcon;
    [SerializeField] private Sprite m_keyboardReadyUpIcon;

    private List<bool> m_readyPlayers;

    private List<InputAction> m_readyUpActions;

    private Coroutine m_startGameCountdownCoroutine;
    [SerializeField] private float m_startGameCountdownSeconds = 3f;
    
    void Awake()
    {
        m_root = m_screenSpaceUIDoc.rootVisualElement.Q<VisualElement>("root");
        m_docs = new List<UIDocument>();

        m_pressToJoinElements = new List<VisualElement>();
        m_readyUpHoverElements = new List<VisualElement>();
        m_readyUpActions = new List<InputAction>();
        m_readyPlayers = new List<bool>();
    }

    void Start()
    {
        for (int i = 0; i < PlayerSystem.Instance.m_maxNumPlayers; i++)
        {
            //resize the render texture quads to fit the screen properly
            Vector3 cameraPos = Camera.main.transform.position;
            float quadToCameraDistance =
                Vector3.Distance(m_renderTextureQuads[i].transform.position, cameraPos);
            float quadHeightScale = 2f * Mathf.Tan(0.5f * Camera.main.fieldOfView * Mathf.Deg2Rad) *
                                      quadToCameraDistance;
            float quadWidthScale = quadHeightScale * Camera.main.aspect / PlayerSystem.Instance.m_maxNumPlayers;

            m_renderTextureQuads[i].transform.localScale = new Vector3(quadWidthScale, quadHeightScale, 1);
            Vector3 currentPos = m_renderTextureQuads[i].transform.position;
            m_renderTextureQuads[i].transform.position = new Vector3(quadWidthScale * (i - 1.5f), currentPos.y, currentPos.z);
            
            m_docs.Add(m_renderTextureQuads[i].GetComponent<UIDocument>());
            
            m_pressToJoinElements.Add(m_docs[i].rootVisualElement);
            m_pressToJoinElements[i].Q<Label>("player-label").text = $"Player {i + 1}";
        }
        
        PlayerSystem.Instance.m_onPlayerJoin += OnPlayerJoin;
    }

    void OnPlayerJoin(int newPlayerNum)
    {
        m_pressToJoinElements[newPlayerNum - 1].Q<Label>("press-to-join-label").text = "";

        VisualElement newReadyUpHover = m_readyUpHover.Instantiate();
        m_readyUpHoverElements.Add(newReadyUpHover);
        
        m_root.Add(newReadyUpHover);
        
        Vector3 screen = Camera.main.WorldToScreenPoint(m_renderTextureQuads[newPlayerNum - 1].transform.position);
        newReadyUpHover.style.left =
            screen.x;// - (newReadyUpHover.Q<VisualElement>("root").layout.width / 2);
        newReadyUpHover.style.top = (Screen.height - screen.y);// + 200;

        PlayerInput input = PlayerSystem.Instance.m_playerInputObjects[newPlayerNum - 1];

        PlayerSystem.Instance.SwitchToActionMapForPlayer(newPlayerNum, "CharacterSelect");

        InputDevice device = PlayerSystem.Instance.m_playerInputObjects[newPlayerNum - 1].devices[0];
        Image image = new Image();
        if (device is Gamepad)
        {
            image.sprite = m_controllerReadyUpIcon;
        }
        else
        {
            image.sprite = m_keyboardReadyUpIcon;
        }
        
        m_readyPlayers.Add(true);
        
        newReadyUpHover.Q<VisualElement>("button-icon").Add(image);
        
        m_readyUpActions.Add(input.actions["ReadyUp"]);

        m_readyUpActions[newPlayerNum - 1].performed += ctx => { PlayerReadyUpToggle(newPlayerNum); };
        PlayerReadyUpToggle(newPlayerNum);
    }

    private void PlayerReadyUpToggle(int playerNum)
    {
        m_readyPlayers[playerNum - 1] = !m_readyPlayers[playerNum - 1];
        Color color;
        if (m_readyPlayers[playerNum - 1])
        {
            color = Color.green;
        }
        else
        {
            color = Color.red;
        }
        m_readyUpHoverElements[playerNum - 1].Q<VisualElement>("root").style.backgroundColor = new StyleColor(color);

        bool startGame = true;
        foreach (bool isPlayerReady in m_readyPlayers)
        {
            if (!isPlayerReady)
            {
                startGame = false;
                break;
            }
        }

        if (startGame)
        {
            m_startGameCountdownCoroutine = StartCoroutine(StartGameCountdown());
        }
        else
        {
            if (m_startGameCountdownCoroutine != null)
            {
                StopCoroutine(m_startGameCountdownCoroutine);
                m_startGameCountdownCoroutine = null;
            }
        }
    }

    private IEnumerator StartGameCountdown()
    {
        yield return new WaitForSeconds(m_startGameCountdownSeconds);
        SceneManager.LoadScene(m_gameSceneToLoadName);
    }
}
