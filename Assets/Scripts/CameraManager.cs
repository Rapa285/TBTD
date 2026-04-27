using UnityEngine;

public class CameraManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject MainCamera;

    private GameObject currCamera;

    private void Awake()
    {
        if (MainCamera == null)
        {
            GameObject camera = GameObject.Find("Main Camera");
            if (camera == null)
            {
                Debug.Log("Tidak ada Main Camera dalam scene");
            }
            else
            {
                MainCamera = camera;
            }
        }
        if (currCamera == null){
            currCamera = MainCamera;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnCameraChangeRequest += changeCamera;
    }

    private void OnDisable()
    {
        GameEvents.OnCameraChangeRequest -= changeCamera;
    }

    public void changeCamera(GameObject nextCamera)
    {
        if (currCamera != null) 
        {
            currCamera.SetActive(false);
        }
        
        currCamera = nextCamera;
        
        if (currCamera != null) 
        {
            currCamera.SetActive(true);
        }
    }
}
