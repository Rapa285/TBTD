using UnityEngine;

/// <summary>
/// Spline-leading launcher that fires an arcing grenade toward a snapshotted destination.
/// </summary>
public sealed class GrenadeLauncherBehaviour : SplineLeadingAttackBehaviour
{
    [SerializeField, Tooltip("Projectile prefab expected to contain an ArchingBullet component.")]
    private GameObject grenadePrefab;

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
        if (target == null || grenadePrefab == null)
        {
            return false;
        }

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        Quaternion spawnRotation = firePoint != null ? firePoint.rotation : transform.rotation;
        GameObject grenadeObject = Instantiate(grenadePrefab, spawnPosition, spawnRotation, projectileParent);

        ArchingBullet projectile = grenadeObject.GetComponent<ArchingBullet>();
        if (projectile == null)
        {
            Debug.LogWarning($"{nameof(GrenadeLauncherBehaviour)} requires a grenade prefab with {nameof(ArchingBullet)}.", this);
            Destroy(grenadeObject);
            return false;
        }

        projectile.Initialize(damage, transform, OwnerTower, this, ProjectileModifiers);
        projectile.SetDestination(GetLeadPosition(target));

        if (!projectile.ReadyToFire())
        {
            Destroy(grenadeObject);
            return false;
        }

        projectile.Fire();
        return true;
    }
}
