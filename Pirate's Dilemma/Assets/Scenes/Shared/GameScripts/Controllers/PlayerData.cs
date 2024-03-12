using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerData : MonoBehaviour
{
    public int m_playerNum;
    public int m_teamNum;
    [FormerlySerializedAs("m_goldCarried")] public int m_bombsCarried;
}
