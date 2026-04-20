// Test concrete weapon that fires a BaseStraightProjectile prefab.
// It inherits spline-aware leading, uses an optional firePoint as the muzzle,
// and exists as a wiring example for future projectile attack behaviours.
using UnityEngine;

/// <summary>
/// Example spline-leading projectile weapon that fires a BaseStraightProjectile prefab.
/// </summary>
public sealed class TestGunAttackBehaviour : SplineLeadingAttackBehaviour
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

    protected override void ExecuteAttack(Transform target, float damage)
    {
        if (target == null || bulletPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        Quaternion spawnRotation = firePoint != null ? firePoint.rotation : transform.rotation;
        GameObject bulletObject = Instantiate(bulletPrefab, spawnPosition, spawnRotation, projectileParent);

        BaseStraightProjectile projectile = bulletObject.GetComponent<BaseStraightProjectile>();
        if (projectile == null)
        {
            Debug.LogWarning($"{nameof(TestGunAttackBehaviour)} requires a bullet prefab with {nameof(BaseStraightProjectile)}.", this);
            Destroy(bulletObject);
            return;
        }

        projectile.Initialize(damage, transform, OwnerTower, this, ProjectileModifiers);
        // GetLeadPosition includes the shared Aim Modifier Vector as the final endpoint offset.
        projectile.SetDirection(GetLeadPosition(target));
        projectile.Fire();
    }
}
