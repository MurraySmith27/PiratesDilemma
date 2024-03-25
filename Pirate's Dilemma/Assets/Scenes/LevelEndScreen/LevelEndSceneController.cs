using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Splines;


public delegate void ImpactBackgroundAppearEvent();

public delegate void ImpactBackgroundDisappearEvent();

public class LevelEndSceneController : MonoBehaviour
{
    [SerializeField] private float m_platformRiseTime;

    [SerializeField] private AnimationCurve m_platformRiseAnimationCurve;

    [SerializeField] private GameObject m_platformGameObject;
    
    [SerializeField] private Transform m_finalPlatformPosition;

    [SerializeField] private List<Transform> m_winnerPositions;
    
    [SerializeField] private List<Transform> m_loserPositions;

    [SerializeField] private float m_sceneTransitionTime;

    [SerializeField] private GameObject m_camera;

    [SerializeField] private float m_cameraAnimTime;

    [SerializeField] private float m_virtualCameraFinalFov;
    
    [SerializeField] private AnimationCurve m_cameraAnimationCurve;

    [SerializeField] private float m_timeSlowdownDuration;

    [SerializeField] private GameObject m_impactBackgroundGameObject;

    [SerializeField] private GameObject m_impactBackgroundParticleObject;

    [SerializeField] private float m_impactBackgroundFadeTime = 0.1f;

    [SerializeField] private float m_cameraArrivalBufferTime = 0.2f;
    
    [SerializeField] private List<GameObject> m_fireWorksParticles;

    [SerializeField] private float m_delayBeforeFireworks = 1f;

    public ImpactBackgroundAppearEvent m_onImpactBackgroundAppear;

    public ImpactBackgroundDisappearEvent m_onImpactBackgroundDisappear;
    
    [HideInInspector] public int m_winningTeamNum;

    
    
    
    public void StartEndLevelScene()
    {
        StartCoroutine(LevelEndSceneCoroutine());
    }

    private IEnumerator LevelEndSceneCoroutine()
    {
        List<GameObject> winnerVisualStandIns = new List<GameObject>();
        //move winning team num to their podium spot, losing team num to the render texture frames.
        for (int i = 0; i < PlayerSystem.Instance.m_numPlayers; i++)
        {
            Transform playerParent = PlayerSystem.Instance.m_playersParents[i];
            
            GameObject player = PlayerSystem.Instance.m_players[i];

            GameObject visualStandIn = null;
            
            for (int j = 0; j < playerParent.transform.childCount; j++)
            {
                Transform child = playerParent.transform.GetChild(j);
                if (child.CompareTag("VisualStandIn"))
                {
                    visualStandIn = child.gameObject;
                }
            }
            
            visualStandIn.SetActive(true);

            if (player.GetComponent<PlayerData>().m_teamNum == m_winningTeamNum)
            {
                
                visualStandIn.transform.position = m_winnerPositions[i / 2].position;
                visualStandIn.transform.localScale = m_winnerPositions[i / 2].localScale;
                visualStandIn.transform.rotation = m_winnerPositions[i / 2].rotation;
                visualStandIn.transform.parent = m_platformGameObject.transform;
                visualStandIn.GetComponent<Animator>().SetTrigger("WonGame");
                winnerVisualStandIns.Add(visualStandIn);
            }
            else
            {
                visualStandIn.transform.position = m_loserPositions[i / 2].position;
                visualStandIn.transform.localScale = m_loserPositions[i / 2].localScale;
                visualStandIn.transform.rotation = m_loserPositions[i / 2].rotation;
                visualStandIn.GetComponent<Animator>().SetTrigger("LostGame");
            }
        }

        yield return new WaitForSeconds(m_sceneTransitionTime);

        float camera_t = 0f;
        
        float initialPlatformHeight = m_platformGameObject.transform.position.y;
        float finalPlatformHeight = m_finalPlatformPosition.position.y;
        
        CinemachineVirtualCamera vcam = m_camera.GetComponent<CinemachineVirtualCamera>();
        float initialCameraFov = vcam.m_Lens.FieldOfView;
        
        float t = 0f;
        while (t < m_platformRiseTime)
        {
            float progress = m_platformRiseAnimationCurve.Evaluate(t / m_platformRiseTime);
            
            float currentPlatformHeight = initialPlatformHeight * (1f - progress) + finalPlatformHeight * progress;
            
            m_platformGameObject.transform.position = new Vector3(
                m_platformGameObject.transform.position.x, 
                currentPlatformHeight, 
                m_platformGameObject.transform.position.z
                );

            float cameraProgress = m_cameraAnimationCurve.Evaluate(camera_t / m_cameraAnimTime);

            var dolly = m_camera.GetComponent<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTrackedDolly>();
            dolly.m_PathPosition = 1f - cameraProgress;
            
            yield return null;
            t += Time.deltaTime;
            camera_t += Time.deltaTime;
        }


        t = 0f;
        while (t < m_cameraAnimTime - m_platformRiseTime)
        {
            float cameraProgress = m_cameraAnimationCurve.Evaluate((camera_t + t) / m_cameraAnimTime);
            
            var dolly = m_camera.GetComponent<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTrackedDolly>();
            dolly.m_PathPosition = 1f - cameraProgress;
            
            vcam.m_Lens.FieldOfView = initialCameraFov * (1f-t / (m_cameraAnimTime - m_platformRiseTime)) + m_virtualCameraFinalFov * t / (m_cameraAnimTime - m_platformRiseTime);
            
            yield return null;
            t += Time.deltaTime;
        }

        yield return new WaitForSeconds(m_cameraArrivalBufferTime);
        
        //at the end slow down frames a lot
        Time.timeScale = 0.1f;
        m_onImpactBackgroundAppear();
        m_impactBackgroundGameObject.SetActive(true);
        m_impactBackgroundParticleObject.SetActive(true);
        foreach (GameObject visualStandIn in winnerVisualStandIns)
        {
            visualStandIn.layer = LayerMask.NameToLayer("LevelEndVisualStandIn");
            for (int child = 0; child < visualStandIn.transform.childCount; child++)
            {
                visualStandIn.transform.GetChild(child).gameObject.layer = LayerMask.NameToLayer("LevelEndVisualStandIn");
            }
        }
        
        yield return new WaitForSeconds(m_timeSlowdownDuration);
        Time.timeScale = 1f;
        
        List<Material> materials = new List<Material>();
        m_impactBackgroundGameObject.GetComponent<Renderer>().GetMaterials(materials);
        t = 0;
        while (t < m_impactBackgroundFadeTime)
        {
            float progress = t / m_impactBackgroundFadeTime;
            materials[0].SetFloat("_Opacity", 1f - progress);
            
            vcam.m_Lens.FieldOfView = m_virtualCameraFinalFov * (1f-progress) + initialCameraFov * progress;
            
            yield return null;
            t += Time.deltaTime;
        }
        
        materials[0].SetFloat("_Opacity", 0f);
        
        foreach (GameObject visualStandIn in winnerVisualStandIns)
        {
            visualStandIn.layer = LayerMask.NameToLayer("HasSelfIntersectingOutline2");
            for (int child = 0; child < visualStandIn.transform.childCount; child++)
            {
                visualStandIn.transform.GetChild(child).gameObject.layer = LayerMask.NameToLayer("HasSelfIntersectingOutline2");
            }
        }
        
        m_impactBackgroundGameObject.SetActive(false);
        m_impactBackgroundParticleObject.SetActive(false);
        m_onImpactBackgroundDisappear();

        yield return new WaitForSeconds(m_delayBeforeFireworks);

        foreach (GameObject fireworkParticle in m_fireWorksParticles)
        {
            fireworkParticle.SetActive(true);
            yield return new WaitForSeconds(0.2f);
        }

    }
}
