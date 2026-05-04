using UnityEngine;

/// <summary>
/// Production spline-leading projectile weapon that fires a BaseStraightProjectile prefab.
/// </summary>
public sealed class BaseGunBehaviour : SplineLeadingAttackBehaviour
{
    [SerializeField, Tooltip("Projectile prefab expected to contain a BaseStraightProjectile component.")]
    private GameObject bulletPrefab;

    [SerializeField, Tooltip("Optional muzzle transform used as the projectile spawn origin.")]
    private Transform firePoint;

    [SerializeField, Tooltip("Optional parent assigned to spawned projectile instances.")]
    private Transform projectileParent;

    protected override Vector3 GetAttackOrigin()
    {
        return firePoint != null ? firePoint.position : transform.position;
    }

    protected override bool ExecuteAttack(Transform target, float damage)
    {
        if (target == null || bulletPrefab == null)
        {
            return false;
        }

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        Quaternion spawnRotation = firePoint != null ? firePoint.rotation : transform.rotation;
        GameObject bulletObject = Instantiate(bulletPrefab, spawnPosition, spawnRotation, projectileParent);

        BaseStraightProjectile projectile = bulletObject.GetComponent<BaseStraightProjectile>();
        if (projectile == null)
        {
            Debug.LogWarning($"{nameof(BaseGunBehaviour)} requires a bullet prefab with {nameof(BaseStraightProjectile)}.", this);
            Destroy(bulletObject);
            return false;
        }

        projectile.Initialize(damage, transform, OwnerTower, this, ProjectileModifiers);
        Vector3 aimPoint = GetLeadPosition(target);
        projectile.SetDirection(aimPoint);

        if (!projectile.ReadyToFire())
        {
            Destroy(bulletObject);
            return false;
        }

        projectile.Fire();
        return true;
    }
}
