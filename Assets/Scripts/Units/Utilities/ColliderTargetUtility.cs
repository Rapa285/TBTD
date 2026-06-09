using UnityEngine;

/// <summary>
/// Shared collider-to-target transform resolution for trigger-driven unit systems.
/// </summary>
public static class ColliderTargetUtility
{
    public static Transform GetTargetTransform(Collider collider)
    {
        if (collider == null)
        {
            return null;
        }

        return collider.attachedRigidbody != null
            ? collider.attachedRigidbody.transform
            : ResolveRootTarget(collider);
    }

    private static Transform ResolveRootTarget(Collider collider)
    {
        HealthComponent health = collider.GetComponentInParent<HealthComponent>();
        if (health != null)
        {
            return health.transform;
        }

        EnemyEntity enemy = collider.GetComponentInParent<EnemyEntity>();
        if (enemy != null)
        {
            return enemy.transform;
        }

        return collider.transform;
    }
}
