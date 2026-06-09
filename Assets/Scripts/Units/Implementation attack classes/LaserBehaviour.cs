using UnityEngine;

/// <summary>
/// Spawns a timed beam projectile that owns laser width checks and damage ticks.
/// </summary>
public sealed class LaserBehaviour : AttackBehaviour
{
    [SerializeField, Tooltip("Projectile type expected to resolve to a BeamProjectile.")]
    private ProjectileType projectileType = ProjectileType.LaserBeam;

    [SerializeField, Tooltip("Optional muzzle transform used as the beam start. Falls back to this transform.")]
    private Transform firePoint;

    [SerializeField, Tooltip("Layers this laser ray is allowed to hit.")]
    private LayerMask hitLayers = ~0;

    [SerializeField, Min(1), Tooltip("Number of parallel rays cast across the beam width.")]
    private int rayResolution = 5;

    [SerializeField, Min(0f), Tooltip("World-space width covered by the parallel beam rays.")]
    private float beamWidth = 0.75f;

    [SerializeField, Min(1), Tooltip("Number of damage ticks used to distribute each attack's total damage.")]
    private int damageTicks = 5;

    private void OnValidate()
    {
        rayResolution = Mathf.Max(1, rayResolution);
        beamWidth = Mathf.Max(0f, beamWidth);
        damageTicks = Mathf.Max(1, damageTicks);
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
        float duration = GetBeamDuration();
        Vector3 widthReferenceAxis = firePoint != null ? firePoint.right : transform.right;
        Quaternion rotation = Quaternion.LookRotation(direction, ResolveBeamUpAxis(direction, widthReferenceAxis));
        if (!TryRequestProjectile(projectileType, start, rotation, out BeamProjectile beamProjectile))
        {
            return false;
        }

        beamProjectile.ConfigureBeam(
            start,
            direction,
            widthReferenceAxis,
            range,
            duration,
            rayResolution,
            beamWidth,
            damageTicks,
            hitLayers);

        beamProjectile.Initialize(damage, OwnerRoot, OwnerTower, this, ProjectileModifiers);

        if (!beamProjectile.ReadyToFire())
        {
            beamProjectile.CancelProjectile();
            return false;
        }

        beamProjectile.Fire();
        if (!beamProjectile.Fired)
        {
            beamProjectile.CancelProjectile();
            return false;
        }

        return true;
    }

    private float GetBeamRange()
    {
        if (OwnerTower != null)
        {
            return Mathf.Max(0.01f, OwnerTower.GetStat(ENTITY_STATS.VisualRange));
        }

        return 10f;
    }

    private float GetBeamDuration()
    {
        if (OwnerTower != null)
        {
            return Mathf.Max(0.01f, OwnerTower.GetStat(ENTITY_STATS.AttackSpeed));
        }

        return 1f;
    }

    private Vector3 GetBeamStart()
    {
        return firePoint != null ? firePoint.position : transform.position;
    }

    private static Vector3 ResolveBeamUpAxis(Vector3 direction, Vector3 widthReferenceAxis)
    {
        Vector3 widthAxis = Vector3.ProjectOnPlane(widthReferenceAxis, direction);
        if (widthAxis.sqrMagnitude <= Mathf.Epsilon)
        {
            widthAxis = Vector3.Cross(direction, Vector3.up);
        }

        Vector3 upAxis = Vector3.Cross(widthAxis, direction);
        return upAxis.sqrMagnitude > Mathf.Epsilon ? upAxis.normalized : Vector3.up;
    }
}
