using UnityEngine;

public abstract class StatusEffect
{
    public string EffectName;
    public float Duration;
    public float ElapsedTime;

    public virtual void OnApply(GameObject target) { }

    public virtual void OnTick(GameObject target, float deltaTime) { }

    public virtual void OnRemove(GameObject target) { }
}
