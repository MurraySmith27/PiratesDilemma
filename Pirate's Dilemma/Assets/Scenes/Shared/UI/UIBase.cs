using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class UIBase : MonoBehaviour
{

    [SerializeField] protected List<string> m_systemDependencies;

    protected static Dictionary<string, bool> m_systemDependenciesReady = new Dictionary<string, bool>();

    private bool m_setUpUIAlready = false;
    
    protected virtual void Awake()
    {
        GameSystem.m_onSetupComplete += SystemReady;

        m_setUpUIAlready = false;
        SetUpIfReady();
    }

    private void SetUpIfReady()
    {
        foreach (string dependency in m_systemDependencies)
        {
            if (!m_systemDependenciesReady.ContainsKey(dependency) || !m_systemDependenciesReady[dependency])
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

    private void SystemReady(string dependencyName)
    {
        if (!m_systemDependenciesReady.ContainsKey(dependencyName))
        {
            m_systemDependenciesReady.Add(dependencyName, true);
        }
        else
        {
            m_systemDependenciesReady[dependencyName] = true;
        }
        SetUpIfReady();
    }

    protected abstract void SetUpUI();
}
