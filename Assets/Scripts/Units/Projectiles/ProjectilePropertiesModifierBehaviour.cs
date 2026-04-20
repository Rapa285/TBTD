using UnityEngine;

/// <summary>
/// Generic projectile modifier for authored changes to common non-damage projectile properties.
/// </summary>
public sealed class ProjectilePropertiesModifierBehaviour : ProjectileModifierBehaviour
{
    [SerializeField, Min(0f), Tooltip("Multiplier applied to projectile lifetime when initialized.")]
    private float maxAgeMultiplier = 1f;

    [SerializeField, Tooltip("Flat value added to projectile lifetime before the multiplier is applied.")]
    private float maxAgeAdd;

    [SerializeField, Tooltip("When enabled, replaces the projectile's destroy-on-hit setting.")]
    private bool overrideDestroyOnHit;

    [SerializeField, Tooltip("Destroy-on-hit value used when Override Destroy On Hit is enabled.")]
    private bool destroyOnHitValue = true;

    [SerializeField, Min(0f), Tooltip("Multiplier applied to BaseStraightProjectile speed when initialized.")]
    private float straightProjectileSpeedMultiplier = 1f;

    [SerializeField, Tooltip("Flat value added to BaseStraightProjectile speed before the multiplier is applied.")]
    private float straightProjectileSpeedAdd;

    [SerializeField, Min(0f), Tooltip("Multiplier applied to supported collider dimensions when initialized.")]
    private float colliderSizeMultiplier = 1f;

    [SerializeField, Tooltip("Flat value added to supported collider dimensions before the multiplier is applied.")]
    private float colliderSizeAdd;

    protected override void ExecuteProjectileInitialized(ProjectileModifierContext context)
    {
        BaseProjectile projectile = context.Projectile;
        if (projectile == null)
        {
            return;
        }

        projectile.MaxAge = Mathf.Max(0f, (projectile.MaxAge + maxAgeAdd) * maxAgeMultiplier);

        if (overrideDestroyOnHit)
        {
            projectile.DestroyOnHit = destroyOnHitValue;
        }

        if (projectile is BaseStraightProjectile straightProjectile)
        {
            straightProjectile.BulletSpeed = (straightProjectile.BulletSpeed + straightProjectileSpeedAdd)
                * straightProjectileSpeedMultiplier;
        }

        ApplyColliderSize(projectile.CollisionCollider);
    }

    private void ApplyColliderSize(Collider collider)
    {
        if (collider == null || (Mathf.Approximately(colliderSizeMultiplier, 1f) && Mathf.Approximately(colliderSizeAdd, 0f)))
        {
            return;
        }

        switch (collider)
        {
            case SphereCollider sphere:
                sphere.radius = Mathf.Max(0f, (sphere.radius + colliderSizeAdd) * colliderSizeMultiplier);
                break;
            case CapsuleCollider capsule:
                capsule.radius = Mathf.Max(0f, (capsule.radius + colliderSizeAdd) * colliderSizeMultiplier);
                break;
            case BoxCollider box:
                Vector3 add = Vector3.one * colliderSizeAdd;
                box.size = Vector3.Max(Vector3.zero, (box.size + add) * colliderSizeMultiplier);
                break;
        }
    }
}
