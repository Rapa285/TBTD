public sealed class DirectAttackBehaviour : AttackBehaviour
{
    protected override void ExecuteAttack(UnityEngine.Transform target, float damage)
    {
        TryApplyDamage(target, damage);
    }
}
