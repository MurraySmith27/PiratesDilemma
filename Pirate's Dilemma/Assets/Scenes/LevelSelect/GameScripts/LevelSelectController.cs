using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

public class LevelSelectController : MonoBehaviour
{

    [SerializeField] private Animator m_animator;

    [SerializeField] private AnimationCurve m_betweenLevelDotsAnimationCurve;

    [SerializeField] private SplineContainer m_levelSelectSpline;

    [SerializeField] private int m_numLevels;

    [SerializeField] private float m_timeToMoveBetweenLevels = 1f;

    [SerializeField] private GameObject m_shipMeshObject;
    
    private int m_currentLevelNum;

    private InputAction m_selectAction;
    private InputAction m_backAction;
    private InputAction m_moveAction;

    private PlayerInput m_player1PlayerInput;

    private Coroutine m_moveCoroutine;
        
    void Start()
    {
        m_currentLevelNum = 1;
        m_player1PlayerInput = PlayerSystem.Instance.m_players[0].GetComponent<PlayerInput>();

        m_player1PlayerInput.actions.FindActionMap("UI").Enable();
        m_player1PlayerInput.actions.FindActionMap("UI").Enable();

        m_selectAction = m_player1PlayerInput.actions.FindAction("Select");
        m_selectAction.performed += EnterCurrentlySelectedLevel;

        m_backAction = m_player1PlayerInput.actions.FindAction("Back");
        m_backAction.performed += BackToCharacterSelect;
        
        m_moveAction = m_player1PlayerInput.actions.FindAction("Move");
        m_moveAction.performed += OnMovePerformed;

        transform.position = m_levelSelectSpline.transform.GetChild(0).position;
    }

    void OnDestroy()
    {
        m_player1PlayerInput.actions.FindAction("Select").performed -= EnterCurrentlySelectedLevel;
        m_player1PlayerInput.actions.FindAction("Back").performed -= EnterCurrentlySelectedLevel;
        m_player1PlayerInput.actions.FindAction("Move").performed -= OnMovePerformed;
    }
    
    private void EnterCurrentlySelectedLevel(InputAction.CallbackContext ctx)
    {
        if (m_currentLevelNum != -1)
        {
            GameTimerSystem.Instance.StopGame(GameTimerSystem.Instance.m_levelSceneNames[m_currentLevelNum-1]);
        }
    }

    private void BackToCharacterSelect(InputAction.CallbackContext ctx)
    {
        GameTimerSystem.Instance.StopGame(GameTimerSystem.Instance.m_characterSelectSceneName);   
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        if (m_currentLevelNum != -1)
        {
            Vector2 moveAmount = m_moveAction.ReadValue<Vector2>();
            Vector3 currentLevelDotPosition = m_levelSelectSpline.EvaluatePosition((m_currentLevelNum - 1f) / (m_numLevels - 1f));

            Vector3 nextLevelPosition = m_levelSelectSpline.EvaluatePosition(m_currentLevelNum / (m_numLevels - 1f));
            Vector3 directionToNext = nextLevelPosition - currentLevelDotPosition;

            Vector2 flattenedDirectionToNext = new Vector2(directionToNext.x, directionToNext.z);
            
            Vector3 lastLevelPosition = m_levelSelectSpline.EvaluatePosition((m_currentLevelNum - 2f) / (m_numLevels - 1f));
            Vector3 directionToLast = lastLevelPosition - currentLevelDotPosition;

            Vector2 flattenedDirectionToLast = new Vector2(directionToLast.x, directionToLast.z);
            
            Debug.Log($"m_currentLevelNum: {m_currentLevelNum}");
            
            Debug.Log($"next dot product: {Vector2.Dot(flattenedDirectionToNext.normalized, moveAmount.normalized)}");
            
            Debug.Log($"prev dot product: {Vector2.Dot(flattenedDirectionToLast.normalized, moveAmount.normalized)}");

            if (Vector2.Dot(flattenedDirectionToNext.normalized, moveAmount.normalized) >
                Vector2.Dot(flattenedDirectionToLast.normalized, moveAmount.normalized))
            {
                //move next
                if (m_currentLevelNum < m_numLevels)
                {
                    if (m_moveCoroutine != null)
                    {
                        StopCoroutine(m_moveCoroutine);
                    }
                    m_moveCoroutine = StartCoroutine(MoveToLevel(m_currentLevelNum + 1));
                }
            }
            else
            {
                //move previous
                if (m_currentLevelNum > 1)
                {
                    if (m_moveCoroutine != null)
                    {
                        StopCoroutine(m_moveCoroutine);
                    }
                    m_moveCoroutine = StartCoroutine(MoveToLevel(m_currentLevelNum - 1));
                }
            }
        }
    }
    

    private IEnumerator MoveToLevel(int levelNum)
    {
        int prevLevelNum = m_currentLevelNum;
        m_currentLevelNum = -1;
        
        //dots are distributed evenly along spline.
        float initialPercentageAlongSpline = (prevLevelNum - 1f) / (m_numLevels - 1);
        float finalPercentageAlongSpline = (levelNum - 1f) / (m_numLevels - 1);
        
        m_animator.SetTrigger("IsMoving");

        float t = 0;
        while (t <= m_timeToMoveBetweenLevels)
        {
            float progress = m_betweenLevelDotsAnimationCurve.Evaluate(t / m_timeToMoveBetweenLevels);
            
            if (progress > 0.9f)
            {
                m_currentLevelNum = levelNum;
            }
            
            float currentPercentageAlongSpline =
                progress * (finalPercentageAlongSpline - initialPercentageAlongSpline) + initialPercentageAlongSpline;

            Vector3 lookDirection = m_levelSelectSpline.EvaluateTangent(currentPercentageAlongSpline);

            if (levelNum > prevLevelNum)
            {
                lookDirection = -lookDirection;
            }

            if (lookDirection != Vector3.zero)
            {
                m_shipMeshObject.transform.rotation = Quaternion.LookRotation(lookDirection);
            }
            
            transform.position = m_levelSelectSpline.EvaluatePosition(currentPercentageAlongSpline);
            
            yield return null;
            t += Time.deltaTime;
        }
        
        m_animator.SetTrigger("IsDoneMoving");

        m_currentLevelNum = levelNum;
    }
}
