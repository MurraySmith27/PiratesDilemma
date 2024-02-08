using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;


public class KrakenController : MonoBehaviour
{

    [SerializeField] private int m_krakenArrivalBuildupSeconds = 5;

    [SerializeField] private float m_krakenRiseHeight;

    [SerializeField] private float m_krakenRiseSeconds;

    [SerializeField] private GameObject m_mainDirectionalLight;

    [SerializeField] private Vector3 m_krakenArrivalLightAngles;

    [SerializeField] private CinemachineVirtualCamera m_vcam;

    [SerializeField] private List<GameObject> m_krakenArrivalDestrucables;

    [SerializeField] private float m_krakenArrivalAgressiveness = 1;
    
    
    public void StartKrakenArrival()
    {
        StartCoroutine(KrakenArrival());
    }
    
    
    private IEnumerator KrakenArrival()
    {

        Quaternion initialLightRotation = m_mainDirectionalLight.transform.rotation;

        Quaternion finalLightRotation = Quaternion.Euler(m_krakenArrivalLightAngles);
        
        
        //begin screen shake
        CameraController cameraController = Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<CameraController>();
        cameraController.StartScreenShake();
        
        //do prep for five seconds
        for (float t = 0; t < 1; t += Time.deltaTime / m_krakenArrivalBuildupSeconds)
        {
            m_mainDirectionalLight.transform.rotation = Quaternion.Slerp(initialLightRotation, finalLightRotation, t);
            yield return null;
        }
        
        //stop screen shake        
        cameraController.StopScreenShake();
        
        //get kraken to move
        for (int i = 0; i < m_krakenArrivalDestrucables.Count; i++)
        {
            m_krakenArrivalDestrucables[i].GetComponent<MeshDestroy>().DestroyMesh();
        }

        Vector3 krakenInitialPos = transform.position;

        Vector3 krakenFinalPos = krakenInitialPos + new Vector3(0, m_krakenRiseHeight, 0);

        Rigidbody krakenRb = GetComponent<Rigidbody>();
        //actually make kraken rise up.
        for (float t = 0; t < 1; t += Time.deltaTime / m_krakenRiseSeconds)
        {
            float aggressiveness = Mathf.Max(m_krakenArrivalAgressiveness + 1, 0);
            float progress = -(t * aggressiveness - aggressiveness) + (t * aggressiveness - (1 / aggressiveness)) + 1;
            Vector3 pos = krakenInitialPos + (krakenFinalPos - krakenInitialPos) * progress;
            krakenRb.MovePosition(pos);
            yield return null;
        }
    }
}