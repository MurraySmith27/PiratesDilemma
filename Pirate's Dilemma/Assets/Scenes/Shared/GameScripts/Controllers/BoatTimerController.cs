using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatTimerController : MonoBehaviour
{
    public BoatSailEvent m_onBoatSail;
    
    private BoatData m_boatData;
    private BoatGoldController m_boatGoldController;
    private Coroutine m_countDownCoroutine;

    [HideInInspector] public int m_currentTimeToLive;
    
    void Start()
    {
        m_boatData = GetComponent<BoatData>();

        m_currentTimeToLive = m_boatData.m_timeToLive;
        
        m_boatGoldController = GetComponent<BoatGoldController>();

        m_boatGoldController.m_onBoatSink += OnBoatSink;
        
        m_countDownCoroutine = StartCoroutine(CountDownTimer());
        
    }

    void OnBoatSink(int teamNum, int boatNum)
    {
        StopCoroutine(m_countDownCoroutine);
    }

    
    IEnumerator CountDownTimer() 
    {

        while (m_currentTimeToLive > 0) {
            m_currentTimeToLive--;
            yield return new WaitForSeconds(1f);
        }
        
        // m_onBoatSail(m_boatData.m_teamNum, m_boatData.m_boatNum, m_boatData.m_currentGoldStored);
    }
}
