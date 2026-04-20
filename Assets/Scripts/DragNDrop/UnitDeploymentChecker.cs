using UnityEngine;
using UnityEngine.Events;

public class UnitDeploymentChecker : MonoBehaviour
{
    public struct PlacementResult
    {
        public bool hasGround;
        public bool isValid;
        public Vector3 position;
        public Vector3 normal;
        public Collider groundCollider;
    }

    [SerializeField] private Camera raycastCamera;
    [SerializeField] private LayerMask groundLayers = 1 << 6;
    [SerializeField] private LayerMask blockingLayers = 1 << 7;
    [SerializeField, Min(0.01f)] private float placementRadius = 0.5f;
    [SerializeField, Min(0.01f)] private float placementHeight = 1f;
    [SerializeField] private float verticalOffset;
    [SerializeField, Min(0.01f)] private float maxRayDistance = 1000f;
    [SerializeField] private UnityEvent onEnterValid = new UnityEvent();
    [SerializeField] private UnityEvent onEnterInvalid = new UnityEvent();

    private bool hasLastPlacementState;
    private bool lastPlacementValid;

    public UnityEvent OnEnterValid => onEnterValid;
    public UnityEvent OnEnterInvalid => onEnterInvalid;

    public bool TryGetPlacement(Vector2 screenPosition, out PlacementResult result)
    {
        result = new PlacementResult
        {
            hasGround = false,
            isValid = false,
            position = Vector3.zero,
            normal = Vector3.up,
            groundCollider = null
        };

        Camera cameraToUse = raycastCamera != null ? raycastCamera : Camera.main;
        if (cameraToUse == null)
        {
            NotifyPlacementState(false);
            return false;
        }

        Ray ray = cameraToUse.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            NotifyPlacementState(false);
            return false;
        }

        bool isBlocked = IsPlacementBlocked(hit.point);
        result = new PlacementResult
        {
            hasGround = true,
            isValid = !isBlocked,
            position = hit.point,
            normal = hit.normal,
            groundCollider = hit.collider
        };

        NotifyPlacementState(result.isValid);
        return result.isValid;
    }

    private bool IsPlacementBlocked(Vector3 position)
    {
        float radius = Mathf.Max(0.01f, placementRadius);
        float height = Mathf.Max(placementHeight, radius * 2f);
        Vector3 basePosition = position + Vector3.up * verticalOffset;
        Vector3 bottom = basePosition + Vector3.up * radius;
        Vector3 top = basePosition + Vector3.up * (height - radius);

        return Physics.CheckCapsule(bottom, top, radius, blockingLayers, QueryTriggerInteraction.Ignore);
    }

    private void NotifyPlacementState(bool isValid)
    {
        if (hasLastPlacementState && lastPlacementValid == isValid)
        {
            return;
        }

        hasLastPlacementState = true;
        lastPlacementValid = isValid;

        if (isValid)
        {
            onEnterValid?.Invoke();
        }
        else
        {
            onEnterInvalid?.Invoke();
        }
    }
}
