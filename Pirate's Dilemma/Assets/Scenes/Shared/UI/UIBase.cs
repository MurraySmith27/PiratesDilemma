using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public abstract class UIBase : MonoBehaviour
{

    [SerializeField] protected List<string> m_systemDependencies;

    protected List<bool> m_systemDependenciesReady;

    private bool m_setUpUIAlready = false;
    
    protected virtual void Awake()
    {
        m_systemDependenciesReady = new List<bool>();
        for (int i = 0; i < m_systemDependencies.Count; i++)
        {
            m_systemDependenciesReady.Add(false);
            string dependency = m_systemDependencies[i];
            Type systemType = Type.GetType(dependency);

            if (systemType != null && !systemType.IsAssignableFrom(typeof(GameSystem)))
            {
                int newInt = i;
                GameSystem.m_onSetupComplete += () => { SystemReady(newInt); };
            }
        }
    }

    private void SystemReady(int systemNum)
    {
        m_systemDependenciesReady[systemNum] = true;

        foreach (bool ready in m_systemDependenciesReady)
        {
            if (!ready)
            {
                return;
            }
        }

        if (!m_setUpUIAlready)
        {
            SetUpUI();
            m_setUpUIAlready = true;
        }
    }

    protected abstract void SetUpUI();
}
