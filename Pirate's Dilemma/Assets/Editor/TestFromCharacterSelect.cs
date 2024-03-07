using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class TestFromCharacterSelect : MonoBehaviour
{

    [MenuItem("PiratesDilemma/StartFromCharacterSelect")]
    static void StartFromCharacterSelect()
    {
        SceneAsset characterSelectScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/CharacterSelect/CharacterSelect.unity");

        EditorSceneManager.playModeStartScene = characterSelectScene;

        EditorApplication.EnterPlaymode();
    }
    
}
