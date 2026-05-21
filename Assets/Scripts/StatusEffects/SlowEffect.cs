using UnityEngine;

public class SlowEffect : StatusEffect
{
    private EnemyMover targetMover;
    private float slowMultiplier;

    public SlowEffect(float slowPercentage, float duration)
    {
        EffectName="Slowdown";
        Duration=duration;
        IsUnique=true;
        EffectStrength=slowPercentage;
        slowMultiplier = Mathf.Clamp01(1f - slowPercentage);

        AutoLoadIcon();
    }

    public override void OnApply(GameObject target)
    {
        if (target==null) return;
        targetMover=target.GetComponent<EnemyMover>();

        if (targetMover != null)
        {
            targetMover.AddSpeedFactor(slowMultiplier);
        }
    }

    public override void OnRemove(GameObject target)
    {
        if (targetMover != null)
        {
            targetMover.RemoveSpeedFactor(slowMultiplier);
        }
        targetMover=null;
    }
}