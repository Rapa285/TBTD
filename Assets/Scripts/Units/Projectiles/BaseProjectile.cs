// Base runtime component for non-hitscan projectiles.
// Owns shared projectile lifetime, trigger-hit filtering, owner ignoring, and damage application;
// derived projectile classes only need to provide movement by overriding TickProjectile.
using System;
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
    private ProjectilePoolService poolService;
    private ProjectileType pooledProjectileType = ProjectileType.None;
    private bool defaultsCached;
    private float defaultDamage;
    private float defaultMaxAge;
    private LayerMask defaultHitLayers;
    private bool defaultDestroyOnHit;
    private ColliderDefaults defaultCollider;

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
    internal ProjectileType PooledProjectileType => pooledProjectileType;
    public event Action<Transform> OnBulletHit;

    protected Collider ProjectileCollider => projectileCollider;

    protected Transform IgnoredRoot => ignoredRoot;

    protected virtual void Awake()
    {
        CacheCollider();
        ConfigureCollider();
        CacheDefaults();
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
        if (!CanHitTarget(target))
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

        float bulletSize = ownerTower != null ? ownerTower.GetStat(ENTITY_STATS.BulletSize) : 1f;
        ApplyBulletSize(Mathf.Max(0f, bulletSize));

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
        if (ApplyProjectileHit(other, target, damage))
        {
            RaiseBulletHit(transform);
        }

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
        RecycleOrDestroy();
    }

    protected virtual void ResetProjectileStateForReuse()
    {
    }

    protected virtual void ApplyBulletSize(float bulletSize)
    {
        if (Mathf.Approximately(bulletSize, 1f))
        {
            return;
        }

        switch (projectileCollider)
        {
            case SphereCollider sphere:
                sphere.radius = Mathf.Max(0f, sphere.radius * bulletSize);
                break;
            case CapsuleCollider capsule:
                capsule.radius = Mathf.Max(0f, capsule.radius * bulletSize);
                capsule.height = Mathf.Max(0f, capsule.height * bulletSize);
                break;
            case BoxCollider box:
                box.size = Vector3.Max(Vector3.zero, box.size * bulletSize);
                break;
        }
    }

    /// <summary>
    /// Cancels a projectile that was requested but could not be fired.
    /// </summary>
    public void CancelProjectile()
    {
        expired = true;
        RecycleOrDestroy();
    }

    internal void ConfigurePoolOwnership(ProjectilePoolService service, ProjectileType projectileType)
    {
        poolService = service;
        pooledProjectileType = projectileType;
    }

    internal void PrepareForPooledUse(ProjectilePoolService service)
    {
        if (poolService == null)
        {
            poolService = service;
        }

        ResetForReuse();
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

    protected void RaiseBulletHit(Transform hitTransform)
    {
        if (hitTransform == null)
        {
            return;
        }

        OnBulletHit?.Invoke(hitTransform);
    }

    protected bool CanHitTarget(Transform target)
    {
        return target != null && !IsIgnoredTarget(target) && IsInHitLayers(target.gameObject);
    }

    protected bool ApplyProjectileHit(Collider hitCollider, Transform target, float damageAmount)
    {
        bool hasHitPosition = hitCollider != null;
        Vector3 hitPosition = hasHitPosition ? hitCollider.ClosestPoint(transform.position) : Vector3.zero;
        return ApplyProjectileHit(hitCollider, target, damageAmount, hitPosition, hasHitPosition);
    }

    protected bool ApplyProjectileHit(
        Collider hitCollider,
        Transform target,
        float damageAmount,
        Vector3 hitPosition,
        bool hasHitPosition)
    {
        if (!CanHitTarget(target))
        {
            return false;
        }

        AttackHitContext context = CreateHitContext(hitCollider, target, damageAmount, hitPosition, hasHitPosition);
        bool damageApplied = TryApplyDamage(target, damageAmount, context);
        // Projectile modifiers run after damage, using the context captured when the shot was fired.
        DispatchProjectileHitModifiers(hitCollider, target, damageAmount, hitPosition, hasHitPosition);
        return damageApplied;
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
        bool hasHitPosition = hitCollider != null;
        Vector3 hitPosition = hasHitPosition ? hitCollider.ClosestPoint(transform.position) : Vector3.zero;
        DispatchProjectileHitModifiers(hitCollider, target, damageAmount, hitPosition, hasHitPosition);
    }

    private void DispatchProjectileHitModifiers(
        Collider hitCollider,
        Transform target,
        float damageAmount,
        Vector3 hitPosition,
        bool hasHitPosition)
    {
        DispatchProjectileModifiers(
            CreateProjectileModifierContext(hitCollider, target, damageAmount, 0f, hitPosition, hasHitPosition),
            ProjectileModifierPhase.Hit);
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
                modifier.gameObject.SetActive(false);
                Destroy(modifier.gameObject);
            }
        }

        projectileModifiers.Clear();
    }

    private void ResetForReuse()
    {
        CacheCollider();
        ConfigureCollider();
        CacheDefaults();
        RestoreDefaults();

        ignoredRoot = null;
        ownerRoot = null;
        ownerTower = null;
        sourceAttackBehaviour = null;
        firedAtTime = 0f;
        fired = false;
        expired = false;
        OnBulletHit = null;
        DestroyProjectileModifiers();
        ResetProjectileStateForReuse();
    }

    private void RecycleOrDestroy()
    {
        if (poolService != null)
        {
            ResetForReuse();
            poolService.ReturnProjectile(this);
            return;
        }

        Destroy(gameObject);
    }

    private void CacheDefaults()
    {
        if (defaultsCached)
        {
            return;
        }

        defaultsCached = true;
        defaultDamage = damage;
        defaultMaxAge = maxAge;
        defaultHitLayers = hitLayers;
        defaultDestroyOnHit = destroyOnHit;
        defaultCollider = ColliderDefaults.Capture(projectileCollider);
    }

    private void RestoreDefaults()
    {
        damage = defaultDamage;
        maxAge = defaultMaxAge;
        hitLayers = defaultHitLayers;
        destroyOnHit = defaultDestroyOnHit;
        defaultCollider.Restore(projectileCollider);
        ConfigureCollider();
    }

    private AttackHitContext CreateHitContext(Collider hitCollider, Transform target, float damageAmount)
    {
        bool hasHitPosition = hitCollider != null;
        Vector3 hitPosition = hasHitPosition ? hitCollider.ClosestPoint(transform.position) : Vector3.zero;
        return CreateHitContext(hitCollider, target, damageAmount, hitPosition, hasHitPosition);
    }

    private AttackHitContext CreateHitContext(
        Collider hitCollider,
        Transform target,
        float damageAmount,
        Vector3 hitPosition,
        bool hasHitPosition)
    {
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
        return CreateProjectileModifierContext(hitCollider, target, damageAmount, deltaTime, hitPosition, hasHitPosition);
    }

    private ProjectileModifierContext CreateProjectileModifierContext(
        Collider hitCollider,
        Transform target,
        float damageAmount,
        float deltaTime,
        Vector3 hitPosition,
        bool hasHitPosition)
    {
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

    protected bool IsInHitLayers(GameObject target)
    {
        return target != null && (hitLayers.value & (1 << target.layer)) != 0;
    }

    private struct ColliderDefaults
    {
        private ColliderKind kind;
        private Vector3 center;
        private Vector3 size;
        private float radius;
        private float height;
        private int direction;

        public static ColliderDefaults Capture(Collider collider)
        {
            ColliderDefaults defaults = new ColliderDefaults();
            switch (collider)
            {
                case SphereCollider sphere:
                    defaults.kind = ColliderKind.Sphere;
                    defaults.center = sphere.center;
                    defaults.radius = sphere.radius;
                    break;
                case CapsuleCollider capsule:
                    defaults.kind = ColliderKind.Capsule;
                    defaults.center = capsule.center;
                    defaults.radius = capsule.radius;
                    defaults.height = capsule.height;
                    defaults.direction = capsule.direction;
                    break;
                case BoxCollider box:
                    defaults.kind = ColliderKind.Box;
                    defaults.center = box.center;
                    defaults.size = box.size;
                    break;
                default:
                    defaults.kind = ColliderKind.Unsupported;
                    break;
            }

            return defaults;
        }

        public void Restore(Collider collider)
        {
            switch (collider)
            {
                case SphereCollider sphere when kind == ColliderKind.Sphere:
                    sphere.center = center;
                    sphere.radius = radius;
                    break;
                case CapsuleCollider capsule when kind == ColliderKind.Capsule:
                    capsule.center = center;
                    capsule.radius = radius;
                    capsule.height = height;
                    capsule.direction = direction;
                    break;
                case BoxCollider box when kind == ColliderKind.Box:
                    box.center = center;
                    box.size = size;
                    break;
            }
        }
    }

    private enum ColliderKind
    {
        Unsupported,
        Sphere,
        Capsule,
        Box
    }
}
