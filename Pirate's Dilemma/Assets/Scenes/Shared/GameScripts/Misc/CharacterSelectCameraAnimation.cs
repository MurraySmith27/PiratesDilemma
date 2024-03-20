using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Splines;

public class CharacterSelectCameraAnimation : MonoBehaviour
{
    [SerializeField] private SplineContainer m_animationSpline;
    
    [SerializeField] private float m_enterSceneAnimationStoppingPercentage = 0.1f;

    [SerializeField] private AnimationCurve m_enterSceneCameraAnimationCurve;
    
    [SerializeField] private float m_enterSceneAnimationDuration = 1f;
    
    [SerializeField] private float m_exitSceneAnimationStoppingPercentage = 0.1f;

    [SerializeField] private AnimationCurve m_exitSceneCameraAnimationCurve;
    
    [SerializeField] private float m_exitSceneAnimationDuration = 1f;


    public void Start()
    {
        StartCoroutine(EnterSceneCameraAnimation());
        
        GameTimerSystem.Instance.m_onCharacterSelectEnd += OnAllPlayersReadyUp;
    }
    
    public void OnDestroy()
    {
        GameTimerSystem.Instance.m_onCharacterSelectEnd -= OnAllPlayersReadyUp;
    }
    
    
    private IEnumerator EnterSceneCameraAnimation()
    {
        float initialPercentage = 0;

        float finalPercentage = m_enterSceneAnimationStoppingPercentage;

        float t = 0;
        while (t <= m_enterSceneAnimationDuration)
        {
            float distanceAlong = m_enterSceneCameraAnimationCurve.Evaluate(t / m_enterSceneAnimationDuration) * finalPercentage;
            Vector3 currentPosition = m_animationSpline.EvaluatePosition(distanceAlong);
            Vector3 lookDirection = m_animationSpline.EvaluateTangent(distanceAlong);
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
            transform.position = currentPosition;
            yield return null;
            t += Time.deltaTime;
        }

        transform.position = m_animationSpline.EvaluatePosition(finalPercentage);
    }

    public void OnAllPlayersReadyUp()
    {
        StartCoroutine(ExitSceneCameraAnimation());
    }
    
    private IEnumerator ExitSceneCameraAnimation()
    {
        float initialPercentage = m_enterSceneAnimationStoppingPercentage;

        float finalPercentage = m_exitSceneAnimationStoppingPercentage;

        float t = 0;
        while (t <= m_exitSceneAnimationDuration)
        {
            float distanceAlong = m_exitSceneCameraAnimationCurve.Evaluate(t / m_exitSceneAnimationDuration) * (finalPercentage - initialPercentage) + initialPercentage;
            Vector3 currentPosition = m_animationSpline.EvaluatePosition(distanceAlong);
            Vector3 lookDirection = m_animationSpline.EvaluateTangent(distanceAlong);
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
            transform.position = currentPosition;
            yield return null;
            t += Time.deltaTime;
        }

        transform.position = m_animationSpline.EvaluatePosition(finalPercentage);
    }
    
    
}
