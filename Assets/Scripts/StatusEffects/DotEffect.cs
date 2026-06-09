using UnityEngine;

public class DotEffect : StatusEffect
{
    private float damagePerTick;
    private float tickInterval;
    private float tickAccumulator;
    private HealthComponent targetHealth;

    public DotEffect(float damagePerTick, int totalTicks, float tickInterval)
    {
        this.EffectName = "Poison";
        this.damagePerTick = damagePerTick;
        this.tickInterval = Mathf.Max(0.0001f, tickInterval);
        this.Duration = totalTicks * this.tickInterval;
        this.ElapsedTime = 0f;
        this.tickAccumulator = 0f;
        AutoLoadIcon();
    }

    public override void OnApply(GameObject target)
    {
        if (target == null) return;
        targetHealth = target.GetComponent<HealthComponent>();
    }

    public override void OnTick(GameObject target, float deltaTime)
    {
        if (target == null) return;

        // If target was destroyed, nothing to do
        if (targetHealth == null)
        {
            targetHealth = target.GetComponent<HealthComponent>();
            if (targetHealth == null) return;
        }

        tickAccumulator += deltaTime;
        while (tickAccumulator >= tickInterval)
        {
            tickAccumulator -= tickInterval;
            if (targetHealth != null && !targetHealth.IsDead)
            {
                targetHealth.TakeDamage(damagePerTick);
            }
            else
            {
                // target dead or missing, schedule removal by setting elapsed to duration
                ElapsedTime = Duration;
                return;
            }
        }
    }

    public override void OnRemove(GameObject target)
    {
        // nothing to cleanup for DOT
        targetHealth = null;
    }
}
