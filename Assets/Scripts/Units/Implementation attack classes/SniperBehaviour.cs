using UnityEngine;

/// <summary>
/// Immediate single-target precision weapon with line-rendered shot feedback.
/// </summary>
public sealed class SniperBehaviour : AttackBehaviour
{
    [SerializeField, Tooltip("Optional muzzle transform used as the shot origin. Falls back to this transform.")]
    private Transform firePoint;

    protected override bool ExecuteAttack(Transform target, float damage)
    {
        if (target == null)
        {
            return false;
        }

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector3 hitPosition = GetAimPoint(target);
        bool damageApplied = TryApplyDamage(target, damage, null, hitPosition, true);

        if (damageApplied)
        {
            AttackFX?.PlayAttackFX(new AttackFXContext(
                this,
                OwnerTower,
                OwnerRoot,
                target,
                damage,
                origin,
                true,
                hitPosition,
                true,
                null));
        }

        return damageApplied;
    }
}
