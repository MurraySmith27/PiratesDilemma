using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[ExecuteInEditMode]
public class CellShadingLightingHelper : MonoBehaviour
{

    private List<GameObject> m_cellShadedMaterialGameObjects = new List<GameObject>();
    private List<GameObject> m_dynamicLightsGameObjects = new List<GameObject>();
    
    void Awake()
    {
        SceneManager.sceneLoaded -= GetAllCellShadedMaterials;
        SceneManager.sceneLoaded += GetAllCellShadedMaterials;
        SceneManager.sceneLoaded -= SetStaticLights;
        SceneManager.sceneLoaded += SetStaticLights;

        DontDestroyOnLoad(this.gameObject);
    }


    private void GetAllCellShadedMaterials(Scene scene, LoadSceneMode mode)
    {
        m_cellShadedMaterialGameObjects.Clear();
        foreach (Renderer renderer in Resources.FindObjectsOfTypeAll<Renderer>())
        {
            List<Material> mats = new();
            renderer.GetSharedMaterials(mats);
            
            if (mats.Count > 0 && mats[0] != null && mats[0].shader != null && mats[0].shader.name.Contains("CellShading"))
            {
                m_cellShadedMaterialGameObjects.Add(renderer.gameObject);
            }
        }

        m_dynamicLightsGameObjects = GameObject.FindGameObjectsWithTag("DynamicPointLight").ToList();
    }

    private void SetStaticLights(Scene scene, LoadSceneMode mode)
    {
        foreach (GameObject cellShadedGameObject in m_cellShadedMaterialGameObjects)
        {
            for (int j = 0; j < 3; j++)
            {
                cellShadedGameObject.GetComponent<Renderer>().sharedMaterial.SetFloat($"_UseLight{j+1}", 0f);
            }
        }
        
        int i = 1;
        foreach (GameObject staticLight in GameObject.FindGameObjectsWithTag("StaticPointLight"))
        {
            foreach (GameObject cellShadedGameObject in m_cellShadedMaterialGameObjects)
            {   
                Light lightComponent = staticLight.GetComponent<Light>();
                
                Renderer renderer = cellShadedGameObject.GetComponent<Renderer>();
                
            renderer.sharedMaterial.SetTexture("_Shading_Ramp", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sprites/UI/Textures/toon-ramp-2.png"));
                renderer.sharedMaterial.SetFloat($"_UseLight{i}", 1f);
                
                renderer.sharedMaterial.SetVector($"_Light{i}Position", staticLight.transform.position);
                
                renderer.sharedMaterial.SetVector($"_Light{i}Color", lightComponent.color);
                
                renderer.sharedMaterial.SetFloat($"_Light{i}Intensity", lightComponent.intensity);
                
                renderer.sharedMaterial.SetFloat($"_Light{i}FlickerStrength", lightComponent.range);
            }
            i++;
        }
    }
    
    private void SetDynamicLights()
    {
        foreach (GameObject cellShadedGameObject in m_cellShadedMaterialGameObjects)
        {
            for (int j = 0; j < 3; j++)
            {
                cellShadedGameObject.GetComponent<Renderer>().sharedMaterial.SetFloat($"_UseLight{j+1}", 0f);
            }
        }
        
        int i = 1;
        foreach (GameObject staticLight in GameObject.FindGameObjectsWithTag("DynamicPointLight"))
        {
            foreach (GameObject cellShadedGameObject in m_cellShadedMaterialGameObjects)
            {   
                Light lightComponent = staticLight.GetComponent<Light>();
                
                Renderer renderer = cellShadedGameObject.GetComponent<Renderer>();
                
                renderer.sharedMaterial.SetTexture("_Shading_Ramp", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sprites/UI/Textures/toon-ramp-2.png"));
                renderer.sharedMaterial.SetFloat($"_UseLight{i}", 1f);
                
                renderer.sharedMaterial.SetVector($"_Light{i}Position", staticLight.transform.position);
                
                renderer.sharedMaterial.SetVector($"_Light{i}Color", lightComponent.color);
                
                renderer.sharedMaterial.SetFloat($"_Light{i}Intensity", lightComponent.intensity);
                
                renderer.sharedMaterial.SetFloat($"_Light{i}FlickerStrength", lightComponent.range);
            }
            i++;
        }
    }
    
    void Update()
    {
        if (Application.isEditor && !Application.isPlaying) {
            GetAllCellShadedMaterials(new Scene(), LoadSceneMode.Additive);
            SetStaticLights(new Scene(), LoadSceneMode.Additive);
        }
        SetDynamicLights();
    }
}
