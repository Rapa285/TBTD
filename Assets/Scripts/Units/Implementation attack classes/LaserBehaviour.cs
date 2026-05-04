using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Piercing hitscan beam that damages each valid target along the beam once.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public sealed class LaserBehaviour : AttackBehaviour
{
    [SerializeField, Tooltip("Line renderer used to draw the temporary laser beam.")]
    private LineRenderer lineRenderer;

    [SerializeField, Tooltip("Optional muzzle transform used as the beam start. Falls back to this transform.")]
    private Transform firePoint;

    [SerializeField, Tooltip("Layers this laser ray is allowed to hit.")]
    private LayerMask hitLayers = ~0;

    [SerializeField, Min(0f), Tooltip("How long the beam remains visible after each attack.")]
    private float beamDuration = 0.08f;

    private Coroutine activeBeam;

    private void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        ConfigureLineRenderer();
    }

    private void OnValidate()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        beamDuration = Mathf.Max(0f, beamDuration);
        ConfigureLineRenderer();
    }

    protected override bool ExecuteAttack(Transform target, float damage)
    {
        if (target == null)
        {
            return false;
        }

        Vector3 start = GetBeamStart();
        Vector3 direction = GetAimPoint(target) - start;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            direction = firePoint != null ? firePoint.forward : transform.forward;
        }

        direction = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector3.forward;
        float range = GetBeamRange();
        Vector3 end = start + direction * range;

        bool hitAnyTarget = DamageTargetsAlongBeam(start, direction, range, damage);
        RenderBeam(start, end);
        return hitAnyTarget;
    }

    private bool DamageTargetsAlongBeam(Vector3 start, Vector3 direction, float range, float damage)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            start,
            direction,
            range,
            hitLayers,
            QueryTriggerInteraction.Collide);

        if (hits.Length == 0)
        {
            return false;
        }

        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        bool hitAnyTarget = false;
        HashSet<Transform> damagedTargets = new HashSet<Transform>();
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            Transform hitTarget = ColliderTargetUtility.GetTargetTransform(hitCollider);
            if (!CanDamageTarget(hitTarget) || !damagedTargets.Add(hitTarget))
            {
                continue;
            }

            hitAnyTarget |= TryApplyDamage(hitTarget, damage, hitCollider, hits[i].point, true);
        }

        return hitAnyTarget;
    }

    private bool CanDamageTarget(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            return false;
        }

        if ((hitLayers.value & (1 << target.gameObject.layer)) == 0)
        {
            return false;
        }

        Transform root = OwnerRoot;
        return root == null || (target != root && !target.IsChildOf(root));
    }

    private float GetBeamRange()
    {
        if (OwnerTower != null)
        {
            return Mathf.Max(0.01f, OwnerTower.GetStat(ENTITY_STATS.VisualRange));
        }

        return 10f;
    }

    private Vector3 GetBeamStart()
    {
        return firePoint != null ? firePoint.position : transform.position;
    }

    private void RenderBeam(Vector3 start, Vector3 end)
    {
        if (lineRenderer == null)
        {
            return;
        }

        if (activeBeam != null)
        {
            StopCoroutine(activeBeam);
        }

        activeBeam = StartCoroutine(RenderBeamRoutine(start, end));
    }

    private IEnumerator RenderBeamRoutine(Vector3 start, Vector3 end)
    {
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        if (beamDuration > 0f)
        {
            yield return new WaitForSeconds(beamDuration);
        }

        lineRenderer.enabled = false;
        activeBeam = null;
    }

    private void ConfigureLineRenderer()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
    }
}
