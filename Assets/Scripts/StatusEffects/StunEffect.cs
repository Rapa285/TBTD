using UnityEngine;

public class StunEffect : StatusEffect
{
    private EnemyMover targetMover;

    public StunEffect(float duration)
    {
        EffectName="Stun";
        Duration=duration;
        AutoLoadIcon();
    }

    public override void OnApply(GameObject target)
    {
        if (target==null) return;
        targetMover=target.GetComponent<EnemyMover>();

        if (targetMover != null)
        {
            targetMover.PauseMovement();
        }
    }

    public override void OnRemove(GameObject target)
    {
        if (targetMover != null)
        {
            targetMover.ResumeMovement();
        }
        targetMover=null;
    }
}