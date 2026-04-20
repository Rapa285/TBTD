using UnityEngine;

/// <summary>
/// Runtime data passed to projectile modifier hooks.
/// </summary>
public readonly struct ProjectileModifierContext
{
    public ProjectileModifierContext(
        BaseProjectile projectile,
        TowerEntity attacker,
        AttackBehaviour sourceAttackBehaviour,
        Transform ownerRoot,
        Transform target,
        Collider hitCollider,
        float damage,
        Vector3 hitPosition,
        bool hasHitPosition,
        float deltaTime)
    {
        Projectile = projectile;
        Attacker = attacker;
        SourceAttackBehaviour = sourceAttackBehaviour;
        OwnerRoot = ownerRoot;
        Target = target;
        HitCollider = hitCollider;
        Damage = damage;
        HitPosition = hitPosition;
        HasHitPosition = hasHitPosition;
        DeltaTime = deltaTime;
    }

    public BaseProjectile Projectile { get; }
    public TowerEntity Attacker { get; }
    public AttackBehaviour SourceAttackBehaviour { get; }
    public Transform OwnerRoot { get; }
    public Transform Target { get; }
    public Collider HitCollider { get; }
    public float Damage { get; }
    public Vector3 HitPosition { get; }
    public bool HasHitPosition { get; }
    public float DeltaTime { get; }

    public Vector3 BestHitPosition => HasHitPosition
        ? HitPosition
        : Target != null ? Target.position : Vector3.zero;
}
