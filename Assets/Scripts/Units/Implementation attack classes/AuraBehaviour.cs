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
            return target != null && TryApplyDamage(target, damage);
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

            damagedAnyTarget |= TryApplyDamage(candidate, damage);
        }

        return damagedAnyTarget;
    }
}
