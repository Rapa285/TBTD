using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public sealed class UnitVision : MonoBehaviour
{
    [SerializeField, Min(0f)] private float range = 5f;
    [SerializeField] private LayerMask targetLayers = ~0;

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
        TryAddTarget(GetTargetTransform(other));
    }

    private void OnTriggerExit(Collider other)
    {
        RemoveTarget(GetTargetTransform(other));
    }

    public Transform GetFirstValidTarget()
    {
        PruneInvalidTargets();
        return validTargets.Count > 0 ? validTargets[0] : null;
    }

    public bool Contains(Transform target)
    {
        PruneInvalidTargets();
        return target != null && validTargets.Contains(target);
    }

    private void TryAddTarget(Transform target)
    {
        if (target == null || !IsInTargetLayer(target.gameObject) || validTargets.Contains(target))
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

    private Transform GetTargetTransform(Collider other)
    {
        if (other == null)
        {
            return null;
        }

        return other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
    }

    private bool IsInTargetLayer(GameObject target)
    {
        return (targetLayers.value & (1 << target.layer)) != 0;
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
}
