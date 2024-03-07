using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.EditorCoroutines.Editor;
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

    [MenuItem("PiratesDilemma/FourPlayerSimulation")]
    static void FourPlayerSimulation()
    {
        SceneAsset characterSelectScene =
            AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/CharacterSelect/CharacterSelect.unity");

        EditorSceneManager.playModeStartScene = characterSelectScene;

        EditorCoroutineUtility.StartCoroutineOwnerless(FourPlayerSimulationCoroutine());
        EditorApplication.EnterPlaymode();


    }
    
    static IEnumerator FourPlayerSimulationCoroutine()
    {

        Debug.Log("four player simulation coroutine");
        yield return new EditorWaitForSeconds(3f);
        for (int i = 0; i < 4; i++)
        {
            InputAction.CallbackContext ctx = new();
            
            Debug.Log("pressing join button!");
            PlayerSystem.Instance.OnJoinButtonPressed(ctx);
        }

        for (int i = 0; i < 4; i++)
        {
            InputAction.CallbackContext ctx = new();
            
            Debug.Log("pressing ready button!");
            PlayerSystem.Instance.OnReadyUpButtonPressed(i+1);
        }
    }
}
