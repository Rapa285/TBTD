using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Basic damage receiver interface for targets that only need the final damage value.
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount);
}

/// <summary>
/// Damage receiver interface for targets that also need hit source context.
/// </summary>
public interface IAttackContextDamageable
{
    void TakeDamage(float amount, AttackHitContext context);
}

/// <summary>
/// Base class for tower weapons called by TowerEntity.
/// </summary>
public abstract class AttackBehaviour : MonoBehaviour
{
    [SerializeField, Min(0f), Tooltip("Base damage before TowerEntity applies its global damage multiplier.")]
    private float baseDamage = 1f;

    private IReadOnlyList<OnHitEffectBehaviour> onHitEffects;
    private TowerEntity ownerTower;
    private Transform ownerRoot;

    public float BaseDamage
    {
        get => baseDamage;
        set => baseDamage = Mathf.Max(0f, value);
    }

    protected TowerEntity OwnerTower => ownerTower;
    protected Transform OwnerRoot => ownerRoot != null ? ownerRoot : transform;
    protected IReadOnlyList<OnHitEffectBehaviour> OnHitEffects => onHitEffects;

    /// <summary>
    /// Configures tower ownership and active on-hit effects for this runtime weapon.
    /// </summary>
    public void ConfigureRuntime(
        TowerEntity tower,
        Transform root,
        IReadOnlyList<OnHitEffectBehaviour> effects)
    {
        ownerTower = tower;
        ownerRoot = root != null ? root : transform;
        onHitEffects = effects;
    }

    /// <summary>
    /// Public attack entrypoint. TowerEntity provides the target and compiled damage multiplier.
    /// </summary>
    public void Attack(Transform target, float damageMultiplier)
    {
        if (target == null)
        {
            return;
        }

        ExecuteAttack(target, baseDamage * Mathf.Max(0f, damageMultiplier));
    }

    protected abstract void ExecuteAttack(Transform target, float damage);

    /// <summary>
    /// Applies damage to a target and dispatches active on-hit effects when the hit resolves.
    /// </summary>
    protected bool TryApplyDamage(Transform target, float damage)
    {
        return TryApplyDamage(target, damage, null, Vector3.zero, false);
    }

    protected bool TryApplyDamage(
        Transform target,
        float damage,
        Collider hitCollider,
        Vector3 hitPosition,
        bool hasHitPosition)
    {
        if (target == null)
        {
            return false;
        }

        AttackHitContext context = CreateHitContext(target, damage, hitCollider, hitPosition, hasHitPosition);

        // Prefer context-aware damage receivers, then fall back to legacy damage and SendMessage support.
        IAttackContextDamageable contextDamageable = target.GetComponentInParent<IAttackContextDamageable>();
        if (contextDamageable != null)
        {
            contextDamageable.TakeDamage(damage, context);
            DispatchOnHitEffects(context);
            return true;
        }

        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            DispatchOnHitEffects(context);
            return true;
        }

        target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        DispatchOnHitEffects(context);
        return true;
    }

    /// <summary>
    /// Dispatches active on-hit effects for custom attack implementations that create their own hit context.
    /// </summary>
    protected void DispatchOnHitEffects(
        Transform target,
        float damage,
        Collider hitCollider,
        Vector3 hitPosition,
        bool hasHitPosition)
    {
        DispatchOnHitEffects(CreateHitContext(target, damage, hitCollider, hitPosition, hasHitPosition));
    }

    /// <summary>
    /// Dispatches active on-hit effects with a prebuilt context.
    /// </summary>
    protected void DispatchOnHitEffects(AttackHitContext context)
    {
        if (onHitEffects == null || onHitEffects.Count == 0 || context.Target == null)
        {
            return;
        }

        for (int i = 0; i < onHitEffects.Count; i++)
        {
            OnHitEffectBehaviour effect = onHitEffects[i];
            if (effect != null)
            {
                effect.ApplyHitEffect(context);
            }
        }
    }

    private AttackHitContext CreateHitContext(
        Transform target,
        float damage,
        Collider hitCollider,
        Vector3 hitPosition,
        bool hasHitPosition)
    {
        return new AttackHitContext(
            ownerTower,
            OwnerRoot,
            this,
            null,
            target,
            hitCollider,
            damage,
            hitPosition,
            hasHitPosition);
    }
}
