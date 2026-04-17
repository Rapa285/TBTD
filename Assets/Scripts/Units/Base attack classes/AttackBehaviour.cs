using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float amount);
}

public abstract class AttackBehaviour : MonoBehaviour
{
    [SerializeField, Min(0f)] private float baseDamage = 1f;

    public float BaseDamage
    {
        get => baseDamage;
        set => baseDamage = Mathf.Max(0f, value);
    }

    public void Attack(Transform target, float damageMultiplier)
    {
        if (target == null)
        {
            return;
        }

        ExecuteAttack(target, baseDamage * Mathf.Max(0f, damageMultiplier));
    }

    protected abstract void ExecuteAttack(Transform target, float damage);

    protected bool TryApplyDamage(Transform target, float damage)
    {
        if (target == null)
        {
            return false;
        }

        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            return true;
        }

        target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        return true;
    }
}
