public sealed class DirectAttackBehaviour : AttackBehaviour
{
    protected override bool ExecuteAttack(UnityEngine.Transform target, float damage)
    {
        return TryApplyDamage(target, damage);
    }
}
