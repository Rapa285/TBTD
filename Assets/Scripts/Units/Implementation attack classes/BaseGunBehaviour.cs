using UnityEngine;

/// <summary>
/// Production spline-leading projectile weapon that fires a BaseStraightProjectile prefab.
/// </summary>
public sealed class BaseGunBehaviour : SplineLeadingAttackBehaviour
{
    [SerializeField, Tooltip("Projectile type expected to resolve to a BaseStraightProjectile.")]
    private ProjectileType projectileType = ProjectileType.Bullet;

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
        if (!TryRequestProjectile(projectileType, spawnPosition, spawnRotation, out BaseStraightProjectile projectile))
        {
            return false;
        }

        projectile.Initialize(damage, transform, OwnerTower, this, ProjectileModifiers);
        Vector3 aimPoint = GetLeadPosition(target, projectile.BulletSpeed);
        projectile.SetDirection(aimPoint);

        if (!projectile.ReadyToFire())
        {
            projectile.CancelProjectile();
            return false;
        }

        projectile.Fire();
        return true;
    }
}
