using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debug utility that spawns an enemy prefab at the mouse hit point when Shift + left click is pressed.
/// </summary>
public sealed class DebugEnemySpawner : MonoBehaviour
{
    [SerializeField, Tooltip("Enemy prefab spawned by the debug Shift + left click input.")]
    private GameObject enemyPrefab;

    [SerializeField, Tooltip("Optional parent assigned to spawned enemy instances.")]
    private Transform spawnParent;

    [SerializeField, Tooltip("Camera used for mouse raycasts. Falls back to Camera.main when unset.")]
    private Camera raycastCamera;

    [SerializeField, Tooltip("Layers considered valid spawn surfaces.")]
    private LayerMask groundLayers = 1 << 6;

    [SerializeField, Min(0.01f), Tooltip("Maximum mouse raycast distance.")]
    private float maxRayDistance = 1000f;

    [SerializeField, Tooltip("Offset applied along the hit normal after finding the spawn point.")]
    private float surfaceOffset;

    [SerializeField, Tooltip("Rotate spawned enemies so their local up axis matches the clicked surface normal.")]
    private bool alignToSurfaceNormal;

    private void Update()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        if (mouse == null || keyboard == null || enemyPrefab == null)
        {
            return;
        }

        if (!mouse.leftButton.wasPressedThisFrame || !IsShiftHeld(keyboard))
        {
            return;
        }

        if (TryGetSpawnPose(mouse.position.ReadValue(), out Vector3 position, out Quaternion rotation))
        {
            Instantiate(enemyPrefab, position, rotation, spawnParent);
        }
    }

    private bool TryGetSpawnPose(Vector2 screenPosition, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        Camera cameraToUse = raycastCamera != null ? raycastCamera : Camera.main;
        if (cameraToUse == null)
        {
            return false;
        }

        Ray ray = cameraToUse.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        position = hit.point + hit.normal * surfaceOffset;
        rotation = alignToSurfaceNormal
            ? Quaternion.FromToRotation(Vector3.up, hit.normal)
            : Quaternion.identity;
        return true;
    }

    private static bool IsShiftHeld(Keyboard keyboard)
    {
        return keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
    }
}
