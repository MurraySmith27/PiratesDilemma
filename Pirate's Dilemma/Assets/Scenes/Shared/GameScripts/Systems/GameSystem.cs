using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public delegate void SystemSetupCompleteEvent();
public abstract class GameSystem : MonoBehaviour
{
    public static SystemSetupCompleteEvent m_onSetupComplete;

    protected virtual void Start()
    {
        if (m_onSetupComplete != null && m_onSetupComplete.GetInvocationList().Length > 0)
        {
            m_onSetupComplete();
        }
    }
}
