// Test concrete weapon that fires a BaseStraightProjectile prefab.
// It inherits spline-aware leading, uses an optional firePoint as the muzzle,
// and exists as a wiring example for future projectile attack behaviours.
using UnityEngine;

public sealed class TestGunAttackBehaviour : SplineLeadingAttackBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform projectileParent;

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

        projectile.Initialize(damage, transform);
        projectile.SetDirection(GetLeadPosition(target));
        projectile.Fire();
    }
}
