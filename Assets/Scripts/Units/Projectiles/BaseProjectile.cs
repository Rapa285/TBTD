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
    private readonly List<ProjectileModifierBehaviour> projectileModifiers = new List<ProjectileModifierBehaviour>();
    private float firedAtTime;
    private bool fired;
    private bool expired;

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

    public bool DestroyOnHit
    {
        get => destroyOnHit;
        set => destroyOnHit = value;
    }

    public bool Fired => fired;
    public Collider CollisionCollider => projectileCollider;

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

        float deltaTime = Time.deltaTime;
        DispatchProjectileTickModifiers(deltaTime);
        TickProjectile(deltaTime);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!fired || other == null)
        {
            return;
        }

        Transform target = ColliderTargetUtility.GetTargetTransform(other);
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
    /// Initializes damage, owner ignoring, and projectile modifiers used by upgraded attacks.
    /// </summary>
    public virtual void Initialize(
        float projectileDamage,
        Transform owner,
        TowerEntity tower,
        AttackBehaviour attackBehaviour,
        IReadOnlyList<ProjectileModifierBehaviour> projectileModifierPrefabs)
    {
        Damage = projectileDamage;
        ownerRoot = owner;
        ignoredRoot = owner != null ? owner.root : null;
        ownerTower = tower;
        sourceAttackBehaviour = attackBehaviour;
        expired = false;
        DestroyProjectileModifiers();

        InstantiateProjectileModifiers(projectileModifierPrefabs);
        DispatchProjectileInitializedModifiers();
    }

    private void InstantiateProjectileModifiers(IReadOnlyList<ProjectileModifierBehaviour> projectileModifierPrefabs)
    {
        if (projectileModifierPrefabs == null)
        {
            return;
        }

        for (int i = 0; i < projectileModifierPrefabs.Count; i++)
        {
            ProjectileModifierBehaviour modifierPrefab = projectileModifierPrefabs[i];
            if (modifierPrefab == null)
            {
                continue;
            }

            ProjectileModifierBehaviour modifierInstance = Instantiate(modifierPrefab, transform);
            modifierInstance.name = $"{modifierPrefab.name} (Projectile Modifier Runtime)";
            projectileModifiers.Add(modifierInstance);
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
        // Projectile modifiers run after damage, using the context captured when the shot was fired.
        DispatchProjectileHitModifiers(other, target, damage);

        if (destroyOnHit)
        {
            Expire();
        }
    }

    protected virtual void Expire()
    {
        if (expired)
        {
            return;
        }

        expired = true;
        DispatchProjectileExpiredModifiers();
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

        return CombatDamageUtility.TryApplyDamage(target, damageAmount, context);
    }

    private void DispatchProjectileInitializedModifiers()
    {
        DispatchProjectileModifiers(CreateProjectileModifierContext(null, null, Damage, 0f), ProjectileModifierPhase.Initialized);
    }

    private void DispatchProjectileTickModifiers(float deltaTime)
    {
        DispatchProjectileModifiers(CreateProjectileModifierContext(null, null, Damage, deltaTime), ProjectileModifierPhase.Tick);
    }

    private void DispatchProjectileHitModifiers(Collider hitCollider, Transform target, float damageAmount)
    {
        DispatchProjectileModifiers(CreateProjectileModifierContext(hitCollider, target, damageAmount, 0f), ProjectileModifierPhase.Hit);
    }

    private void DispatchProjectileExpiredModifiers()
    {
        DispatchProjectileModifiers(CreateProjectileModifierContext(null, null, Damage, 0f), ProjectileModifierPhase.Expired);
    }

    private void DispatchProjectileModifiers(ProjectileModifierContext context, ProjectileModifierPhase phase)
    {
        if (projectileModifiers.Count == 0)
        {
            return;
        }

        for (int i = 0; i < projectileModifiers.Count; i++)
        {
            ProjectileModifierBehaviour modifier = projectileModifiers[i];
            if (modifier == null)
            {
                continue;
            }

            switch (phase)
            {
                case ProjectileModifierPhase.Initialized:
                    modifier.ApplyProjectileInitialized(context);
                    break;
                case ProjectileModifierPhase.Tick:
                    modifier.ApplyProjectileTick(context);
                    break;
                case ProjectileModifierPhase.Hit:
                    modifier.ApplyProjectileHit(context);
                    break;
                case ProjectileModifierPhase.Expired:
                    modifier.ApplyProjectileExpired(context);
                    break;
            }
        }
    }

    private void DestroyProjectileModifiers()
    {
        for (int i = 0; i < projectileModifiers.Count; i++)
        {
            ProjectileModifierBehaviour modifier = projectileModifiers[i];
            if (modifier != null)
            {
                Destroy(modifier.gameObject);
            }
        }

        projectileModifiers.Clear();
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

    private ProjectileModifierContext CreateProjectileModifierContext(
        Collider hitCollider,
        Transform target,
        float damageAmount,
        float deltaTime)
    {
        bool hasHitPosition = hitCollider != null;
        Vector3 hitPosition = hasHitPosition ? hitCollider.ClosestPoint(transform.position) : Vector3.zero;

        return new ProjectileModifierContext(
            this,
            ownerTower,
            sourceAttackBehaviour,
            ownerRoot,
            target,
            hitCollider,
            damageAmount,
            hitPosition,
            hasHitPosition,
            deltaTime);
    }

    private enum ProjectileModifierPhase
    {
        Initialized,
        Tick,
        Hit,
        Expired
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
