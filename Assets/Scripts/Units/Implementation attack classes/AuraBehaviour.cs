using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Area pulse weapon that damages every currently tracked target in its tower vision range.
/// </summary>
public sealed class AuraBehaviour : AttackBehaviour
{
    public override bool RequiresCooldownWhenTargetsFirstAvailable => true;

    protected override bool ExecuteAttack(Transform target, float damage)
    {
        UnitVision vision = OwnerTower != null ? OwnerTower.Vision : null;
        if (vision == null)
        {
            if (target == null || !TryApplyDamage(target, damage))
            {
                return false;
            }

            PlayAuraHitFX(target, damage);
            return true;
        }

        if (!vision.HasValidTargets)
        {
            return false;
        }

        List<Transform> targets = new List<Transform>(vision.ValidTargets);
        bool damagedAnyTarget = false;
        for (int i = 0; i < targets.Count; i++)
        {
            Transform candidate = targets[i];
            if (candidate == null || !vision.Contains(candidate))
            {
                continue;
            }

            if (TryApplyDamage(candidate, damage))
            {
                PlayAuraHitFX(candidate, damage);
                damagedAnyTarget = true;
            }
        }

        return damagedAnyTarget;
    }

    private void PlayAuraHitFX(Transform target, float damage)
    {
        AttackFX?.PlayAttackFX(new AttackFXContext(
            this,
            OwnerTower,
            OwnerRoot,
            target,
            damage,
            transform.position,
            true,
            GetAimPoint(target),
            true,
            null));
    }
}
