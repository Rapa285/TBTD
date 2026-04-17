// Base runtime component for non-hitscan projectiles.
// Owns shared projectile lifetime, trigger-hit filtering, owner ignoring, and damage application;
// derived projectile classes only need to provide movement by overriding TickProjectile.
using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class BaseProjectile : MonoBehaviour
{
    [SerializeField, Min(0f)] private float damage = 1f;
    [SerializeField, Min(0f)] private float maxAge = 5f;
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField] private bool destroyOnHit = true;
    [SerializeField] private Collider projectileCollider;

    private Transform ignoredRoot;
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

    public virtual void Initialize(float projectileDamage, Transform owner)
    {
        Damage = projectileDamage;
        ignoredRoot = owner != null ? owner.root : null;
    }

    public virtual bool ReadyToFire()
    {
        return projectileCollider != null && maxAge >= 0f;
    }

    public virtual void Fire()
    {
        if (!ReadyToFire())
        {
            return;
        }

        fired = true;
        firedAtTime = Time.time;
    }

    protected virtual void TickProjectile(float deltaTime)
    {
    }

    protected virtual void OnHit(Collider other, Transform target)
    {
        TryApplyDamage(target, damage);

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
        if (target == null)
        {
            return false;
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
