using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class ModelViewer : MonoBehaviour, IDragHandler
{
    [Header("Model Settings")]
    public Vector3 modelOffset = new Vector3(0, -1f, 3f);
    public Vector3 modelRotation = new Vector3(0, 180f, 0);
    public float cameraFOV = 25f;
    public float modelScaleMultiplier = 1f;
    public float rotationSpeed = -0.5f;

    private RawImage rawImage;
    private Camera hiddenCamera;
    private RenderTexture renderTexture;
    private GameObject spawnedModel;

    public void DisplayModel(EnemyDataSO enemyData)
    {
        ClearModel();

        if (enemyData == null || enemyData.displayModelPrefab == null)
        {
            Debug.LogWarning("AutoUIModelViewer: No model to display for " + (enemyData != null ? enemyData.enemyName : "null data"));
            return;
        }

        rawImage = GetComponent<RawImage>();

        renderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
        renderTexture.Create();
        rawImage.texture = renderTexture;
        rawImage.enabled = true;

        GameObject camObj = new GameObject("Camera_" + gameObject.name);
        camObj.transform.position = new Vector3(0, -9999f, 0); 
        
        hiddenCamera = camObj.AddComponent<Camera>();
        hiddenCamera.cameraType = CameraType.Preview;
        hiddenCamera.clearFlags = CameraClearFlags.SolidColor;
        hiddenCamera.backgroundColor = new Color(0, 0, 0, 0); 
        hiddenCamera.cullingMask = 1 << 31; 
        hiddenCamera.fieldOfView = cameraFOV;
        hiddenCamera.targetTexture = renderTexture;

        spawnedModel = Instantiate(enemyData.displayModelPrefab, hiddenCamera.transform.position + modelOffset, Quaternion.identity);
        spawnedModel.transform.SetParent(hiddenCamera.transform);
        
        spawnedModel.transform.localRotation = Quaternion.Euler(modelRotation);
        spawnedModel.transform.localScale *= modelScaleMultiplier;

        foreach (var cam in spawnedModel.GetComponentsInChildren<Camera>(true)) Destroy(cam.gameObject);
        foreach (var l in spawnedModel.GetComponentsInChildren<Light>(true)) Destroy(l.gameObject);
        foreach (var a in spawnedModel.GetComponentsInChildren<AudioListener>(true)) Destroy(a.gameObject);

        Renderer enemyRenderer = spawnedModel.GetComponentInChildren<Renderer>(); 
        if (enemyRenderer != null)
        {
            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
            enemyRenderer.GetPropertyBlock(propBlock, 1);
            propBlock.SetColor("_BaseColor", enemyData.typeColor);
            enemyRenderer.SetPropertyBlock(propBlock, 1);
        }

        SetLayerRecursively(spawnedModel, 31);
    }

    public void ClearModel()
    {
        if (hiddenCamera != null) Destroy(hiddenCamera.gameObject);
        if (spawnedModel != null) Destroy(spawnedModel);
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
        if (rawImage != null)
        {
            rawImage.texture = null;
            rawImage.enabled = false;
        }
    }

    private void OnDisable()
    {
        ClearModel();
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (spawnedModel != null)
        {
            float dragAmount = eventData.delta.x;
            
            spawnedModel.transform.Rotate(Vector3.up, dragAmount * rotationSpeed, Space.World);
        }
    }
}