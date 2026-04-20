using UnityEngine;

/// <summary>
/// Base class for upgrade-authored hit and projectile behavior modifiers.
/// </summary>
/// <remarks>
/// Non-projectile attacks such as direct damage, laser, beam, and hitscan weapons should still
/// consider dispatching the hit hook through AttackBehaviour.TryApplyDamage or DispatchHitModifiers
/// when upgrade-authored hit behavior should apply. In those cases the context has no projectile.
/// </remarks>
public abstract class ProjectileModifierBehaviour : MonoBehaviour
{
    public void ApplyProjectileInitialized(ProjectileModifierContext context)
    {
        if (isActiveAndEnabled)
        {
            ExecuteProjectileInitialized(context);
        }
    }

    public void ApplyProjectileTick(ProjectileModifierContext context)
    {
        if (isActiveAndEnabled)
        {
            ExecuteProjectileTick(context);
        }
    }

    public void ApplyProjectileHit(ProjectileModifierContext context)
    {
        if (isActiveAndEnabled)
        {
            ExecuteProjectileHit(context);
        }
    }

    public void ApplyProjectileExpired(ProjectileModifierContext context)
    {
        if (isActiveAndEnabled)
        {
            ExecuteProjectileExpired(context);
        }
    }

    protected virtual void ExecuteProjectileInitialized(ProjectileModifierContext context)
    {
    }

    protected virtual void ExecuteProjectileTick(ProjectileModifierContext context)
    {
    }

    protected virtual void ExecuteProjectileHit(ProjectileModifierContext context)
    {
    }

    protected virtual void ExecuteProjectileExpired(ProjectileModifierContext context)
    {
    }
}
