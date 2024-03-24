using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelEndSceneController : MonoBehaviour
{
    [SerializeField] private float m_platformRiseTime;

    [SerializeField] private AnimationCurve m_platformRiseAnimationCurve;

    [SerializeField] private GameObject m_platformGameObject;

    [SerializeField] private List<Transform> m_winnerPositions;
    
    [SerializeField] private List<Transform> m_loserPositions;

    [SerializeField] private float m_sceneTransitionTime;

    public int m_winningTeamNum;

    
    public void StartEndLevelScene()
    {
        StartCoroutine(LevelEndSceneCoroutine());
    }

    private IEnumerator LevelEndSceneCoroutine()
    {
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
                visualStandIn.transform.position = m_winnerPositions[i % 2].position;
                visualStandIn.transform.localScale = m_winnerPositions[i % 2].localScale;
                visualStandIn.transform.rotation = m_winnerPositions[i % 2].rotation;
                visualStandIn.GetComponent<Animator>().SetTrigger("WonGame");
            }
            else
            {
                visualStandIn.transform.position = m_loserPositions[i % 2].position;
                visualStandIn.transform.localScale = m_loserPositions[i % 2].localScale;
                visualStandIn.transform.rotation = m_loserPositions[i % 2].rotation;
                visualStandIn.GetComponent<Animator>().SetTrigger("LostGame");
            }
        }

        yield return new WaitForSeconds(m_sceneTransitionTime);
        
        
    }
}
