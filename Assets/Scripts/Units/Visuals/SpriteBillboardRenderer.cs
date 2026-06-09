using TMPro;
using UnityEngine;

[ExecuteAlways]
public sealed class SpriteBillboardRenderer : MonoBehaviour
{
    private enum BillboardMode
    {
        FaceCamera,
        MatchCameraForward,
        YAxisOnly
    }

    [SerializeField] private Camera targetCamera;
    [SerializeField] private BillboardMode billboardMode = BillboardMode.FaceCamera;
    [SerializeField] private bool invertForward;

    private SpriteRenderer spriteRenderer;
    private TMP_Text tmpText;
    private Camera activeCamera;

    public SpriteRenderer SpriteRenderer => spriteRenderer;
    public TMP_Text TmpText => tmpText;
    public bool HasSupportedRenderer => spriteRenderer != null || tmpText != null;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void LateUpdate()
    {
        ResolveReferences();

        if (!HasSupportedRenderer)
        {
            return;
        }

        activeCamera = GetActiveCamera();

        if (activeCamera == null)
        {
            return;
        }

        ApplyBillboardRotation();
    }

    private void ResolveReferences()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (tmpText == null)
        {
            tmpText = GetComponent<TMP_Text>();
        }
    }

    private Camera GetActiveCamera()
    {
        if (targetCamera != null)
        {
            return targetCamera;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            return mainCamera;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying && UnityEditor.SceneView.lastActiveSceneView != null)
        {
            return UnityEditor.SceneView.lastActiveSceneView.camera;
        }
#endif

        return null;
    }

    private void ApplyBillboardRotation()
    {
        switch (billboardMode)
        {
            case BillboardMode.FaceCamera:
                FaceCamera();
                break;
            case BillboardMode.MatchCameraForward:
                MatchCameraForward();
                break;
            case BillboardMode.YAxisOnly:
                FaceCameraOnYAxis();
                break;
        }
    }

    private void FaceCamera()
    {
        Vector3 direction = activeCamera.transform.position - transform.position;
        SetRotationFromDirection(direction);
    }

    private void MatchCameraForward()
    {
        Vector3 direction = -activeCamera.transform.forward;
        SetRotationFromDirection(direction);
    }

    private void FaceCameraOnYAxis()
    {
        Vector3 direction = activeCamera.transform.position - transform.position;
        direction.y = 0f;
        SetRotationFromDirection(direction);
    }

    private void SetRotationFromDirection(Vector3 direction)
    {
        if (invertForward)
        {
            direction = -direction;
        }

        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }
}
