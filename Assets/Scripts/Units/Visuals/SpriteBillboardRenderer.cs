using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
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
    private Camera activeCamera;

    public SpriteRenderer SpriteRenderer => spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnValidate()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        activeCamera = GetActiveCamera();

        if (activeCamera == null)
        {
            return;
        }

        ApplyBillboardRotation();
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
