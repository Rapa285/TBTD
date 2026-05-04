using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;

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

    [SerializeField, Tooltip("Optional object scaled to the vision diameter for range previews.")]
    private GameObject visualization;

    private readonly List<Transform> validTargets = new List<Transform>();
    private readonly Dictionary<Transform, HealthComponent> targetHealthComponents = new Dictionary<Transform, HealthComponent>();
    private readonly Dictionary<HealthComponent, int> trackedHealthCounts = new Dictionary<HealthComponent, int>();
    private SphereCollider visionCollider;

    public IReadOnlyList<Transform> ValidTargets => validTargets;
    public bool HasValidTargets
    {
        get
        {
            PruneInvalidTargets();
            return validTargets.Count > 0;
        }
    }

    public event UnityAction<Transform> TargetAdded;
    public event UnityAction<Transform> TargetRemoved;
    public event UnityAction TargetsChanged;

    public float Range
    {
        get => range;
        set
        {
            range = Mathf.Max(0f, value);
            SyncCollider();
            SyncVisualization();
        }
    }

    private void Awake()
    {
        visionCollider = GetComponent<SphereCollider>();
        SyncCollider();
        SyncVisualization();
    }

    private void OnValidate()
    {
        range = Mathf.Max(0f, range);
        visionCollider = GetComponent<SphereCollider>();
        SyncCollider();
        SyncVisualization();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryAddTarget(ColliderTargetUtility.GetTargetTransform(other));
    }

    private void OnTriggerExit(Collider other)
    {
        RemoveTarget(ColliderTargetUtility.GetTargetTransform(other));
    }

    private void OnDisable()
    {
        ClearTargets();
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
    /// Returns the tracked spline target furthest along its path, falling back to tracked order.
    /// </summary>
    public Transform GetFrontMostValidTarget()
    {
        PruneInvalidTargets();
        if (validTargets.Count == 0)
        {
            return null;
        }

        Transform fallbackTarget = validTargets[0];
        Transform bestSplineTarget = null;
        float bestProgress = float.NegativeInfinity;

        for (int i = 0; i < validTargets.Count; i++)
        {
            Transform target = validTargets[i];
            if (TryGetSplineProgress(target, out float progress) && progress > bestProgress)
            {
                bestProgress = progress;
                bestSplineTarget = target;
            }
        }

        return bestSplineTarget != null ? bestSplineTarget : fallbackTarget;
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
        if (validTargets.Count == 0)
        {
            return;
        }

        for (int i = validTargets.Count - 1; i >= 0; i--)
        {
            RemoveTargetAt(i);
        }

        TargetsChanged?.Invoke();
    }

    private bool TryGetSplineProgress(Transform target, out float progress)
    {
        progress = 0f;
        if (!TryGetSplineAnimate(target, out SplineAnimate splineAnimate))
        {
            return false;
        }

        progress = splineAnimate.NormalizedTime;
        return true;
    }

    private bool TryGetSplineAnimate(Transform target, out SplineAnimate splineAnimate)
    {
        splineAnimate = null;
        if (target == null)
        {
            return false;
        }

        splineAnimate = target.GetComponent<SplineAnimate>();
        if (splineAnimate != null)
        {
            return true;
        }

        splineAnimate = target.GetComponentInParent<SplineAnimate>();
        if (splineAnimate != null)
        {
            return true;
        }

        splineAnimate = target.GetComponentInChildren<SplineAnimate>();
        return splineAnimate != null;
    }

    private void ClearTargetsWithoutEvents()
    {
        for (int i = validTargets.Count - 1; i >= 0; i--)
        {
            Transform target = validTargets[i];
            UntrackTargetHealth(target);
        }

        validTargets.Clear();
    }

    /// <summary>
    /// Rebuilds tracked targets from an overlap scan. Useful for deployment refreshes and debug polling.
    /// </summary>
    public void ScanForTargetsOnce()
    {
        bool hadTargets = validTargets.Count > 0;
        ClearTargetsWithoutEvents();
        SyncCollider();
        Physics.SyncTransforms();

        bool addedAnyTarget = false;
        Vector3 center = GetWorldCenter();
        float radius = GetWorldRadius();
        Collider[] overlappingColliders = Physics.OverlapSphere(
            center,
            radius,
            ~0,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < overlappingColliders.Length; i++)
        {
            addedAnyTarget |= TryAddTarget(ColliderTargetUtility.GetTargetTransform(overlappingColliders[i]), false);
        }

        if (addedAnyTarget || hadTargets)
        {
            TargetsChanged?.Invoke();
        }
    }

    private bool TryAddTarget(Transform target)
    {
        return TryAddTarget(target, true);
    }

    private bool TryAddTarget(Transform target, bool raiseChanged)
    {
        if (target == null || IsOwnTransform(target) || !IsInTargetLayer(target.gameObject) || validTargets.Contains(target))
        {
            return false;
        }

        validTargets.Add(target);
        TrackTargetHealth(target);
        TargetAdded?.Invoke(target);

        if (raiseChanged)
        {
            TargetsChanged?.Invoke();
        }

        return true;
    }

    private void RemoveTarget(Transform target)
    {
        if (target == null)
        {
            return;
        }

        int targetIndex = validTargets.IndexOf(target);
        if (targetIndex < 0)
        {
            return;
        }

        RemoveTargetAt(targetIndex);
        TargetsChanged?.Invoke();
    }

    private void RemoveTargetAt(int targetIndex)
    {
        Transform target = validTargets[targetIndex];
        validTargets.RemoveAt(targetIndex);
        UntrackTargetHealth(target);
        TargetRemoved?.Invoke(target);
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
        bool removedAnyTarget = false;
        for (int i = validTargets.Count - 1; i >= 0; i--)
        {
            if (IsInvalidTarget(validTargets[i]))
            {
                RemoveTargetAt(i);
                removedAnyTarget = true;
            }
        }

        if (removedAnyTarget)
        {
            TargetsChanged?.Invoke();
        }
    }

    private bool IsInvalidTarget(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            return true;
        }

        return targetHealthComponents.TryGetValue(target, out HealthComponent health)
            && health != null
            && health.IsDead;
    }

    private void TrackTargetHealth(Transform target)
    {
        HealthComponent health = target.GetComponentInParent<HealthComponent>();
        if (health == null)
        {
            return;
        }

        targetHealthComponents[target] = health;
        if (trackedHealthCounts.TryGetValue(health, out int count))
        {
            trackedHealthCounts[health] = count + 1;
            return;
        }

        trackedHealthCounts.Add(health, 1);
        health.OnDeath.AddListener(HandleTrackedTargetDeath);
    }

    private void UntrackTargetHealth(Transform target)
    {
        if (ReferenceEquals(target, null) || !targetHealthComponents.TryGetValue(target, out HealthComponent health))
        {
            return;
        }

        targetHealthComponents.Remove(target);
        if (health == null || !trackedHealthCounts.TryGetValue(health, out int count))
        {
            return;
        }

        count--;
        if (count > 0)
        {
            trackedHealthCounts[health] = count;
            return;
        }

        trackedHealthCounts.Remove(health);
        health.OnDeath.RemoveListener(HandleTrackedTargetDeath);
    }

    private void HandleTrackedTargetDeath()
    {
        PruneInvalidTargets();
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

    private void SyncVisualization()
    {
        if (visualization == null)
        {
            return;
        }

        visualization.transform.localScale = Vector3.one * (range * 2f);
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
