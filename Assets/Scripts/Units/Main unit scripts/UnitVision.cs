using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger-based target tracker used by towers for current-target retention and reacquisition.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public sealed class UnitVision : MonoBehaviour
{
    [SerializeField, Min(0f), Tooltip("Vision radius used to size the trigger sphere and overlap scans.")]
    private float range = 5f;

    [SerializeField, Tooltip("Layers this vision volume is allowed to track as valid targets.")]
    private LayerMask targetLayers = ~0;

    private readonly List<Transform> validTargets = new List<Transform>();
    private SphereCollider visionCollider;

    public IReadOnlyList<Transform> ValidTargets => validTargets;

    public float Range
    {
        get => range;
        set
        {
            range = Mathf.Max(0f, value);
            SyncCollider();
        }
    }

    private void Awake()
    {
        visionCollider = GetComponent<SphereCollider>();
        SyncCollider();
    }

    private void OnValidate()
    {
        range = Mathf.Max(0f, range);
        visionCollider = GetComponent<SphereCollider>();
        SyncCollider();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryAddTarget(ColliderTargetUtility.GetTargetTransform(other));
    }

    private void OnTriggerExit(Collider other)
    {
        RemoveTarget(ColliderTargetUtility.GetTargetTransform(other));
    }

    /// <summary>
    /// Returns the oldest still-valid tracked target after pruning null or inactive entries.
    /// </summary>
    public Transform GetFirstValidTarget()
    {
        PruneInvalidTargets();
        return validTargets.Count > 0 ? validTargets[0] : null;
    }

    /// <summary>
    /// Returns whether the supplied transform is still tracked as a valid target.
    /// </summary>
    public bool Contains(Transform target)
    {
        PruneInvalidTargets();
        return target != null && validTargets.Contains(target);
    }

    /// <summary>
    /// Clears the current tracked-target cache.
    /// </summary>
    public void ClearTargets()
    {
        validTargets.Clear();
    }

    /// <summary>
    /// Rebuilds tracked targets from an overlap scan. Useful for deployment refreshes and debug polling.
    /// </summary>
    public void ScanForTargetsOnce()
    {
        ClearTargets();
        SyncCollider();
        Physics.SyncTransforms();

        Vector3 center = GetWorldCenter();
        float radius = GetWorldRadius();
        Collider[] overlappingColliders = Physics.OverlapSphere(
            center,
            radius,
            ~0,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < overlappingColliders.Length; i++)
        {
            TryAddTarget(ColliderTargetUtility.GetTargetTransform(overlappingColliders[i]));
        }
    }

    private void TryAddTarget(Transform target)
    {
        if (target == null || IsOwnTransform(target) || !IsInTargetLayer(target.gameObject) || validTargets.Contains(target))
        {
            return;
        }

        validTargets.Add(target);
    }

    private void RemoveTarget(Transform target)
    {
        if (target == null)
        {
            return;
        }

        validTargets.Remove(target);
    }

    private bool IsInTargetLayer(GameObject target)
    {
        return (targetLayers.value & (1 << target.layer)) != 0;
    }

    private bool IsOwnTransform(Transform target)
    {
        return target == transform || target.IsChildOf(transform.root);
    }

    private void PruneInvalidTargets()
    {
        for (int i = validTargets.Count - 1; i >= 0; i--)
        {
            if (validTargets[i] == null || !validTargets[i].gameObject.activeInHierarchy)
            {
                validTargets.RemoveAt(i);
            }
        }
    }

    private void SyncCollider()
    {
        if (visionCollider == null)
        {
            return;
        }

        visionCollider.isTrigger = true;
        visionCollider.radius = range;
    }

    private Vector3 GetWorldCenter()
    {
        return visionCollider != null ? visionCollider.bounds.center : transform.position;
    }

    private float GetWorldRadius()
    {
        if (visionCollider == null)
        {
            return range;
        }

        Vector3 scale = visionCollider.transform.lossyScale;
        float largestAxis = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        return visionCollider.radius * largestAxis;
    }
}
