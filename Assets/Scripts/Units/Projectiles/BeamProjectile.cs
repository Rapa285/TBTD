using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stationary raycast beam projectile that owns timed laser damage and projectile modifier lifecycle.
/// </summary>
public sealed class BeamProjectile : BaseProjectile
{
    [SerializeField, Min(1), Tooltip("Number of parallel rays cast across the beam width.")]
    private int rayResolution = 5;

    [SerializeField, Min(0f), Tooltip("World-space width covered by the parallel beam rays.")]
    private float beamWidth = 0.75f;

    [SerializeField, Min(1), Tooltip("Number of damage ticks used to distribute the projectile's total damage.")]
    private int damageTicks = 5;

    [SerializeField, Tooltip("Optional sustained line visual used while this beam is alive.")]
    private LineAttackFXComponent beamFX;

    [SerializeField, Tooltip("Physics trigger behavior used by beam raycasts.")]
    private QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide;

    private readonly List<RaycastHit> rayHits = new List<RaycastHit>();
    private readonly HashSet<Transform> damagedTargetsThisTick = new HashSet<Transform>();
    private Vector3 beamOrigin;
    private Vector3 beamDirection = Vector3.forward;
    private Vector3 beamWidthAxis = Vector3.right;
    private float beamRange = 10f;
    private float beamDuration = 1f;
    private float damagePerTick = 1f;
    private float tickInterval = 0.2f;
    private float nextDamageTickTime;
    private int damageTicksApplied;

    protected override void Awake()
    {
        base.Awake();
        ResolveBeamFX();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        rayResolution = Mathf.Max(1, rayResolution);
        beamWidth = Mathf.Max(0f, beamWidth);
        damageTicks = Mathf.Max(1, damageTicks);
        ResolveBeamFX();
    }

    public void ConfigureBeam(
        Vector3 origin,
        Vector3 direction,
        Vector3 widthReferenceAxis,
        float range,
        float duration,
        int resolution,
        float width,
        int ticks,
        LayerMask layers)
    {
        beamOrigin = origin;
        beamDirection = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector3.forward;
        beamWidthAxis = ResolveWidthAxis(widthReferenceAxis, beamDirection);
        beamRange = Mathf.Max(0.01f, range);
        beamDuration = Mathf.Max(0.01f, duration);
        rayResolution = Mathf.Max(1, resolution);
        beamWidth = Mathf.Max(0f, width);
        damageTicks = Mathf.Max(1, ticks);
        tickInterval = beamDuration / damageTicks;

        transform.position = beamOrigin;
        transform.rotation = Quaternion.LookRotation(beamDirection, ResolveUpAxis(beamDirection, beamWidthAxis));

        MaxAge = beamDuration;
        DestroyOnHit = false;
        HitLayers = layers;
    }

    public override bool ReadyToFire()
    {
        return base.ReadyToFire()
            && beamRange > 0f
            && beamDuration > 0f
            && rayResolution > 0
            && damageTicks > 0
            && beamDirection.sqrMagnitude > Mathf.Epsilon;
    }

    public override void Fire()
    {
        if (!ReadyToFire())
        {
            return;
        }

        damageTicksApplied = 0;
        damagePerTick = Damage / Mathf.Max(1, damageTicks);
        nextDamageTickTime = Time.time;
        PlayBeamFX();

        base.Fire();

        if (Fired)
        {
            ApplyDamageTick();
            nextDamageTickTime = Time.time + tickInterval;
        }
    }

    protected override void OnTriggerEnter(Collider other)
    {
        // Beam damage is resolved by explicit raycast ticks, not by this required trigger collider.
    }

    protected override void TickProjectile(float deltaTime)
    {
        while (damageTicksApplied < damageTicks && Time.time + Mathf.Epsilon >= nextDamageTickTime)
        {
            ApplyDamageTick();
            nextDamageTickTime += tickInterval;
        }
    }

    private void ApplyDamageTick()
    {
        if (damageTicksApplied >= damageTicks)
        {
            return;
        }

        damageTicksApplied++;
        damagedTargetsThisTick.Clear();
        GatherBeamHits();

        if (rayHits.Count == 0)
        {
            return;
        }

        rayHits.Sort((left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < rayHits.Count; i++)
        {
            RaycastHit hit = rayHits[i];
            Collider hitCollider = hit.collider;
            Transform hitTarget = ColliderTargetUtility.GetTargetTransform(hitCollider);
            if (!CanDamageTarget(hitTarget) || !damagedTargetsThisTick.Add(hitTarget))
            {
                continue;
            }

            ApplyProjectileHit(hitCollider, hitTarget, damagePerTick, hit.point, true);
        }
    }

    private void GatherBeamHits()
    {
        rayHits.Clear();

        for (int i = 0; i < rayResolution; i++)
        {
            Vector3 rayOrigin = GetRayOrigin(i);
            RaycastHit[] hits = Physics.RaycastAll(
                rayOrigin,
                beamDirection,
                beamRange,
                HitLayers,
                queryTriggerInteraction);

            if (hits == null || hits.Length == 0)
            {
                continue;
            }

            for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++)
            {
                rayHits.Add(hits[hitIndex]);
            }
        }
    }

    private Vector3 GetRayOrigin(int index)
    {
        if (rayResolution <= 1 || beamWidth <= 0f)
        {
            return beamOrigin;
        }

        float normalizedOffset = (rayResolution == 1)
            ? 0f
            : ((float)index / (rayResolution - 1)) - 0.5f;

        return beamOrigin + beamWidthAxis * (normalizedOffset * beamWidth);
    }

    private bool CanDamageTarget(Transform target)
    {
        return target != null
            && target.gameObject.activeInHierarchy
            && CanHitTarget(target);
    }

    private void PlayBeamFX()
    {
        ResolveBeamFX();
        if (beamFX == null)
        {
            return;
        }

        beamFX.PlaySustainedLine(beamOrigin, beamOrigin + beamDirection * beamRange, beamDuration);
    }

    private void ResolveBeamFX()
    {
        if (beamFX == null)
        {
            beamFX = GetComponent<LineAttackFXComponent>();
        }
    }

    private static Vector3 ResolveWidthAxis(Vector3 referenceAxis, Vector3 direction)
    {
        Vector3 axis = Vector3.ProjectOnPlane(referenceAxis, direction);
        if (axis.sqrMagnitude > Mathf.Epsilon)
        {
            return axis.normalized;
        }

        axis = Vector3.Cross(direction, Vector3.up);
        if (axis.sqrMagnitude > Mathf.Epsilon)
        {
            return axis.normalized;
        }

        axis = Vector3.Cross(direction, Vector3.right);
        return axis.sqrMagnitude > Mathf.Epsilon ? axis.normalized : Vector3.right;
    }

    private static Vector3 ResolveUpAxis(Vector3 direction, Vector3 widthAxis)
    {
        Vector3 up = Vector3.Cross(widthAxis, direction);
        return up.sqrMagnitude > Mathf.Epsilon ? up.normalized : Vector3.up;
    }
}
