using UnityEngine;

/// <summary>
/// Shared damage dispatch for attacks and projectiles.
/// </summary>
public static class CombatDamageUtility
{
    /// <summary>
    /// Applies damage through legacy interface, then SendMessage fallback paths.
    /// </summary>
    public static bool TryApplyDamage(Transform target, float damage)
    {
        return TryApplyDamage(target, damage, default, false);
    }

    /// <summary>
    /// Applies damage through context-aware, legacy interface, then SendMessage fallback paths.
    /// </summary>
    public static bool TryApplyDamage(Transform target, float damage, AttackHitContext context)
    {
        return TryApplyDamage(target, damage, context, true);
    }

    private static bool TryApplyDamage(Transform target, float damage, AttackHitContext context, bool hasContext)
    {
        if (target == null)
        {
            return false;
        }

        if (hasContext)
        {
            IAttackContextDamageable contextDamageable = target.GetComponentInParent<IAttackContextDamageable>();
            if (contextDamageable != null)
            {
                contextDamageable.TakeDamage(damage, context);
                return true;
            }
        }

        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            return true;
        }

        if (!hasContext)
        {
            IAttackContextDamageable contextDamageable = target.GetComponentInParent<IAttackContextDamageable>();
            if (contextDamageable != null)
            {
                contextDamageable.TakeDamage(damage, CreateAnonymousContext(target, damage));
                return true;
            }
        }

        target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        return true;
    }

    private static AttackHitContext CreateAnonymousContext(Transform target, float damage)
    {
        return new AttackHitContext(
            null,
            null,
            null,
            null,
            target,
            null,
            damage,
            Vector3.zero,
            false);
    }
}
