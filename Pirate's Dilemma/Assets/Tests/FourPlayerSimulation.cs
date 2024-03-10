using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class FourPlayerSimulation : InputTestFixture
{
    [OneTimeSetUp]
    public void LoadCharacterSelect()
    {
    }
    
    [Test]
    public void RunFourPlayerSimulation()
    {
        SceneManager.LoadScene("CharacterSelect");
        List<Gamepad> gamePads = new();
        for (int i = 0; i < 4; i++)
        {
            gamePads.Add(InputSystem.AddDevice<Gamepad>());
            Press(gamePads[i].startButton);
            Press(gamePads[i].startButton);
        }

        Assert.That(true);
    }
}
