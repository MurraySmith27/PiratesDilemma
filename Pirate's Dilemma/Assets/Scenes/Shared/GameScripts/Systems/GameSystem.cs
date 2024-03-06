using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public delegate void SystemSetupCompleteEvent(string systemName);
public delegate void SystemDestroyedEvent(string systemName);
public abstract class GameSystem : MonoBehaviour
{
    public static SystemSetupCompleteEvent m_onSetupComplete;
    public static SystemDestroyedEvent m_onSystemDestroyed;
    

    protected void SystemReady()
    {
        if (m_onSetupComplete != null && m_onSetupComplete.GetInvocationList().Length > 0)
        {
            Debug.Log($"SYSTEM: {this.GetType().Name} set up!");
            m_onSetupComplete(this.GetType().Name);
        }
    }

    protected virtual void OnDestroy()
    {
        if (m_onSystemDestroyed != null && m_onSystemDestroyed.GetInvocationList().Length > 0)
        {
            
            Debug.Log($"SYSTEM: {this.GetType().Name} tore down!");
            m_onSystemDestroyed(this.GetType().Name);
        }
    }
}
