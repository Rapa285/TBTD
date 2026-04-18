using UnityEngine;

/// <summary>
/// Immutable data passed to damage receivers and on-hit effects when an attack connects.
/// </summary>
public struct AttackHitContext
{
    /// <summary>
    /// Creates a hit context for direct, hitscan-style, or projectile attacks.
    /// </summary>
    public AttackHitContext(
        TowerEntity attacker,
        Transform attackerRoot,
        AttackBehaviour attackBehaviour,
        BaseProjectile projectile,
        Transform target,
        Collider hitCollider,
        float damage,
        Vector3 hitPosition,
        bool hasHitPosition)
    {
        Attacker = attacker;
        AttackerRoot = attackerRoot;
        AttackBehaviour = attackBehaviour;
        Projectile = projectile;
        Target = target;
        HitCollider = hitCollider;
        Damage = damage;
        HitPosition = hitPosition;
        HasHitPosition = hasHitPosition;
    }

    public TowerEntity Attacker { get; }
    public Transform AttackerRoot { get; }
    public AttackBehaviour AttackBehaviour { get; }
    public BaseProjectile Projectile { get; }
    public Transform Target { get; }
    public Collider HitCollider { get; }
    public float Damage { get; }
    public Vector3 HitPosition { get; }
    public bool HasHitPosition { get; }

    /// <summary>
    /// Returns the explicit hit position when available, otherwise the current target position.
    /// </summary>
    public Vector3 BestHitPosition => HasHitPosition
        ? HitPosition
        : Target != null ? Target.position : Vector3.zero;
}
