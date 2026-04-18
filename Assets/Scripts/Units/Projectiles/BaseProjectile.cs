// Base runtime component for non-hitscan projectiles.
// Owns shared projectile lifetime, trigger-hit filtering, owner ignoring, and damage application;
// derived projectile classes only need to provide movement by overriding TickProjectile.
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base runtime component for trigger-based projectiles with shared hit filtering and damage dispatch.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class BaseProjectile : MonoBehaviour
{
    [SerializeField, Min(0f), Tooltip("Damage applied when this projectile hits a valid target.")]
    private float damage = 1f;

    [SerializeField, Min(0f), Tooltip("Lifetime in seconds after firing. Zero disables age-based expiry.")]
    private float maxAge = 5f;

    [SerializeField, Tooltip("Layers this projectile is allowed to hit.")]
    private LayerMask hitLayers = ~0;

    [SerializeField, Tooltip("Destroy this projectile GameObject after a valid hit.")]
    private bool destroyOnHit = true;

    [SerializeField, Tooltip("Trigger collider used for projectile hit detection.")]
    private Collider projectileCollider;

    private Transform ignoredRoot;
    private Transform ownerRoot;
    private TowerEntity ownerTower;
    private AttackBehaviour sourceAttackBehaviour;
    private readonly List<OnHitEffectBehaviour> onHitEffects = new List<OnHitEffectBehaviour>();
    private float firedAtTime;
    private bool fired;

    public float Damage
    {
        get => damage;
        set => damage = Mathf.Max(0f, value);
    }

    public float MaxAge
    {
        get => maxAge;
        set => maxAge = Mathf.Max(0f, value);
    }

    public LayerMask HitLayers
    {
        get => hitLayers;
        set => hitLayers = value;
    }

    public bool Fired => fired;

    protected Collider ProjectileCollider => projectileCollider;

    protected Transform IgnoredRoot => ignoredRoot;

    protected virtual void Awake()
    {
        CacheCollider();
        ConfigureCollider();
    }

    protected virtual void OnValidate()
    {
        damage = Mathf.Max(0f, damage);
        maxAge = Mathf.Max(0f, maxAge);
        CacheCollider();
        ConfigureCollider();
    }

    protected virtual void Update()
    {
        if (!fired)
        {
            return;
        }

        if (maxAge > 0f && Time.time >= firedAtTime + maxAge)
        {
            Expire();
            return;
        }

        TickProjectile(Time.deltaTime);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!fired || other == null)
        {
            return;
        }

        Transform target = GetTargetTransform(other);
        if (IsIgnoredTarget(target) || !IsInHitLayers(target.gameObject))
        {
            return;
        }

        OnHit(other, target);
    }

    /// <summary>
    /// Initializes damage and owner ignoring for callers that do not need upgrade hit context.
    /// </summary>
    public virtual void Initialize(float projectileDamage, Transform owner)
    {
        Initialize(projectileDamage, owner, null, null, null);
    }

    /// <summary>
    /// Initializes damage, owner ignoring, and the hit-effect context used by upgraded attacks.
    /// </summary>
    public virtual void Initialize(
        float projectileDamage,
        Transform owner,
        TowerEntity tower,
        AttackBehaviour attackBehaviour,
        IReadOnlyList<OnHitEffectBehaviour> effects)
    {
        Damage = projectileDamage;
        ownerRoot = owner;
        ignoredRoot = owner != null ? owner.root : null;
        ownerTower = tower;
        sourceAttackBehaviour = attackBehaviour;
        onHitEffects.Clear();

        // Snapshot the current effect instances so in-flight projectiles do not change when upgrades are added later.
        if (effects == null)
        {
            return;
        }

        for (int i = 0; i < effects.Count; i++)
        {
            OnHitEffectBehaviour effect = effects[i];
            if (effect != null)
            {
                onHitEffects.Add(effect);
            }
        }
    }

    /// <summary>
    /// Returns whether this projectile has enough setup data to be fired.
    /// </summary>
    public virtual bool ReadyToFire()
    {
        return projectileCollider != null && maxAge >= 0f;
    }

    /// <summary>
    /// Starts projectile movement and lifetime tracking.
    /// </summary>
    public virtual void Fire()
    {
        if (!ReadyToFire())
        {
            return;
        }

        fired = true;
        firedAtTime = Time.time;
    }

    /// <summary>
    /// Per-frame movement hook implemented by concrete projectile types.
    /// </summary>
    protected virtual void TickProjectile(float deltaTime)
    {
    }

    protected virtual void OnHit(Collider other, Transform target)
    {
        AttackHitContext context = CreateHitContext(other, target, damage);
        TryApplyDamage(target, damage, context);
        // Projectile effects run after damage, using the context captured when the shot was fired.
        DispatchOnHitEffects(context);

        if (destroyOnHit)
        {
            Expire();
        }
    }

    protected virtual void Expire()
    {
        Destroy(gameObject);
    }

    protected bool TryApplyDamage(Transform target, float damageAmount)
    {
        return TryApplyDamage(target, damageAmount, CreateHitContext(null, target, damageAmount));
    }

    protected bool TryApplyDamage(Transform target, float damageAmount, AttackHitContext context)
    {
        if (target == null)
        {
            return false;
        }

        IAttackContextDamageable contextDamageable = target.GetComponentInParent<IAttackContextDamageable>();
        if (contextDamageable != null)
        {
            contextDamageable.TakeDamage(damageAmount, context);
            return true;
        }

        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damageAmount);
            return true;
        }

        target.SendMessage("TakeDamage", damageAmount, SendMessageOptions.DontRequireReceiver);
        return true;
    }

    protected void DispatchOnHitEffects(Collider hitCollider, Transform target, float damageAmount)
    {
        DispatchOnHitEffects(CreateHitContext(hitCollider, target, damageAmount));
    }

    protected void DispatchOnHitEffects(AttackHitContext context)
    {
        if (onHitEffects.Count == 0 || context.Target == null)
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

    private AttackHitContext CreateHitContext(Collider hitCollider, Transform target, float damageAmount)
    {
        bool hasHitPosition = hitCollider != null;
        Vector3 hitPosition = hasHitPosition ? hitCollider.ClosestPoint(transform.position) : Vector3.zero;

        return new AttackHitContext(
            ownerTower,
            ownerRoot,
            sourceAttackBehaviour,
            this,
            target,
            hitCollider,
            damageAmount,
            hitPosition,
            hasHitPosition);
    }

    protected Transform GetTargetTransform(Collider other)
    {
        if (other == null)
        {
            return null;
        }

        return other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
    }

    protected bool IsIgnoredTarget(Transform target)
    {
        if (target == null)
        {
            return true;
        }

        if (target == transform || target.IsChildOf(transform))
        {
            return true;
        }

        return ignoredRoot != null && (target == ignoredRoot || target.IsChildOf(ignoredRoot));
    }

    private void CacheCollider()
    {
        if (projectileCollider == null)
        {
            projectileCollider = GetComponent<Collider>();
        }
    }

    private void ConfigureCollider()
    {
        if (projectileCollider != null)
        {
            projectileCollider.isTrigger = true;
        }
    }

    private bool IsInHitLayers(GameObject target)
    {
        return target != null && (hitLayers.value & (1 << target.layer)) != 0;
    }
}
