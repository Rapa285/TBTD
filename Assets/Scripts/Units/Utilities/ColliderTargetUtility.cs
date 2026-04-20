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
            : collider.transform;
    }
}
