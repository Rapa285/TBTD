using UnityEngine;

/// <summary>
/// Clears a TrailRenderer when this object is disabled so pooled projectiles do not reuse stale trail points.
/// </summary>
public sealed class CutTrailOnDisable : MonoBehaviour
{
    [SerializeField, Tooltip("Trail cleared when this GameObject is disabled. Defaults to a TrailRenderer on this GameObject.")]
    private TrailRenderer trail;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnDisable()
    {
        if (trail != null)
        {
            trail.Clear();
        }
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (trail == null)
        {
            trail = GetComponent<TrailRenderer>();
        }
    }
}
