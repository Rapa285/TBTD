using UnityEngine;

/// <summary>
/// Spline-leading launcher that fires an arcing grenade toward a snapshotted destination.
/// </summary>
public sealed class GrenadeLauncherBehaviour : SplineLeadingAttackBehaviour
{
    [SerializeField, Tooltip("Projectile type expected to resolve to an ArchingBullet.")]
    private ProjectileType projectileType = ProjectileType.Grenade;

    [SerializeField, Tooltip("Optional muzzle transform used as the projectile spawn origin.")]
    private Transform firePoint;

    protected override Vector3 GetAttackOrigin()
    {
        return firePoint != null ? firePoint.position : transform.position;
    }

    protected override bool ExecuteAttack(Transform target, float damage)
    {
        if (target == null)
        {
            return false;
        }

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        Quaternion spawnRotation = firePoint != null ? firePoint.rotation : transform.rotation;
        if (!TryRequestProjectile(projectileType, spawnPosition, spawnRotation, out ArchingBullet projectile))
        {
            return false;
        }

        projectile.Initialize(damage, transform, OwnerTower, this, ProjectileModifiers);
        projectile.SetDestination(GetPredictedGrenadeDestination(projectile, target));

        if (!projectile.ReadyToFire())
        {
            projectile.CancelProjectile();
            return false;
        }

        projectile.Fire();
        return true;
    }

    private Vector3 GetPredictedGrenadeDestination(ArchingBullet projectile, Transform target)
    {
        Vector3 destination = target.position;
        for (int i = 0; i < 3; i++)
        {
            float flightDuration = projectile.EstimateFlightDuration(destination);
            destination = GetLeadPositionAtTravelTime(target, flightDuration);
        }

        return destination;
    }
}
