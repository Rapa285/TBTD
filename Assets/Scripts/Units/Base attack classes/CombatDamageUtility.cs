using UnityEngine;

/// <summary>
/// Shared damage dispatch for attacks and projectiles.
/// </summary>
public static class CombatDamageUtility
{
    /// <summary>
    /// Applies damage through context-aware, legacy interface, then SendMessage fallback paths.
    /// </summary>
    public static bool TryApplyDamage(Transform target, float damage, AttackHitContext context)
    {
        if (target == null)
        {
            return false;
        }

        IAttackContextDamageable contextDamageable = target.GetComponentInParent<IAttackContextDamageable>();
        if (contextDamageable != null)
        {
            contextDamageable.TakeDamage(damage, context);
            return true;
        }

        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            return true;
        }

        target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        return true;
    }
}
