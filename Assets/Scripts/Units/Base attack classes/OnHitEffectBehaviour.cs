using UnityEngine;

/// <summary>
/// Base class for upgrade-authored effects that run after an attack hits a target.
/// </summary>
public abstract class OnHitEffectBehaviour : MonoBehaviour
{
    /// <summary>
    /// Public effect entrypoint used by attacks and projectiles.
    /// </summary>
    public void ApplyHitEffect(AttackHitContext context)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        ExecuteHitEffect(context);
    }

    /// <summary>
    /// Implements this effect's custom response to a resolved hit.
    /// </summary>
    protected abstract void ExecuteHitEffect(AttackHitContext context);
}
