using UnityEngine;

/// <summary>
/// Implemented by attack visualization components used by AttackBehaviour scripts.
/// </summary>
public interface AttackFXComponent
{
    void PlayAttackFX(AttackFXContext context);
}

public readonly struct AttackFXContext
{
    public AttackFXContext(
        AttackBehaviour sourceAttackBehaviour,
        TowerEntity ownerTower,
        Transform ownerRoot,
        Transform target,
        float damage,
        Vector3 origin,
        bool hasOrigin,
        Vector3 hitPosition,
        bool hasHitPosition,
        Collider hitCollider)
    {
        SourceAttackBehaviour = sourceAttackBehaviour;
        OwnerTower = ownerTower;
        OwnerRoot = ownerRoot;
        Target = target;
        Damage = damage;
        Origin = origin;
        HasOrigin = hasOrigin;
        HitPosition = hitPosition;
        HasHitPosition = hasHitPosition;
        HitCollider = hitCollider;
    }

    public AttackBehaviour SourceAttackBehaviour { get; }
    public TowerEntity OwnerTower { get; }
    public Transform OwnerRoot { get; }
    public Transform Target { get; }
    public float Damage { get; }
    public Vector3 Origin { get; }
    public bool HasOrigin { get; }
    public Vector3 HitPosition { get; }
    public bool HasHitPosition { get; }
    public Collider HitCollider { get; }
}
