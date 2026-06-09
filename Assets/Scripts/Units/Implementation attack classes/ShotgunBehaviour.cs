using UnityEngine;

/// <summary>
/// Spline-leading projectile weapon that fires a randomized spread volley.
/// </summary>
public sealed class ShotgunBehaviour : SplineLeadingAttackBehaviour
{
    [SerializeField, Tooltip("Projectile type expected to resolve to a BaseStraightProjectile.")]
    private ProjectileType projectileType = ProjectileType.Bullet;

    [SerializeField, Tooltip("Optional muzzle transform used as the projectile spawn origin.")]
    private Transform firePoint;

    [SerializeField, Min(1), Tooltip("Number of full-damage projectiles fired per attack tick.")]
    private int pelletCount = 6;

    [SerializeField, Range(0f, 89f), Tooltip("Maximum horizontal yaw deviation from the aimed direction in degrees.")]
    private float spreadAngleDegrees = 30f;

    protected override void OnValidate()
    {
        base.OnValidate();
        pelletCount = Mathf.Max(1, pelletCount);
        spreadAngleDegrees = Mathf.Clamp(spreadAngleDegrees, 0f, 89f);
    }

    protected override Vector3 GetAttackOrigin()
    {
        return firePoint != null ? firePoint.position : transform.position;
    }

    protected override bool ExecuteAttack(Transform target, float damage)
    {
        if (target == null || pelletCount <= 0)
        {
            return false;
        }

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        Quaternion spawnRotation = firePoint != null ? firePoint.rotation : transform.rotation;
        Vector3 aimDirection = Vector3.zero;
        bool hasAimDirection = false;
        bool firedAnyProjectile = false;

        for (int i = 0; i < pelletCount; i++)
        {
            if (!TryRequestProjectile(projectileType, spawnPosition, spawnRotation, out BaseStraightProjectile projectile))
            {
                continue;
            }

            projectile.Initialize(damage, transform, OwnerTower, this, ProjectileModifiers);
            if (!hasAimDirection)
            {
                aimDirection = GetLeadPosition(target, projectile.BulletSpeed) - spawnPosition;
                if (aimDirection.sqrMagnitude <= Mathf.Epsilon)
                {
                    aimDirection = spawnRotation * Vector3.forward;
                }

                aimDirection.Normalize();
                hasAimDirection = true;
            }

            projectile.SetTravelDirection(GetSpreadDirection(aimDirection));

            if (!projectile.ReadyToFire())
            {
                projectile.CancelProjectile();
                continue;
            }

            projectile.Fire();
            firedAnyProjectile = true;
        }

        return firedAnyProjectile;
    }

    private Vector3 GetSpreadDirection(Vector3 aimDirection)
    {
        if (spreadAngleDegrees <= 0f)
        {
            return aimDirection;
        }

        float yawOffset = Random.Range(-spreadAngleDegrees, spreadAngleDegrees);
        return Quaternion.AngleAxis(yawOffset, Vector3.up) * aimDirection.normalized;
    }
}
