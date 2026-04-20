using UnityEngine;

[RequireComponent(typeof(EnemyMover))]
public class SniperAttacker : MonoBehaviour
{
    [Header("Sniper Stats")]
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float attackCooldown = 2f;
    
    [Header("Targeting")]
    [SerializeField] private UnitVision vision;

    private EnemyMover mover;
    private float lastAttackTime;
    private Transform currentTarget;

    private void Awake()
    {
        mover = GetComponent<EnemyMover>();
        
        if (vision == null)
        {
            vision = GetComponentInChildren<UnitVision>();
        }
    }

    private void Update()
    {
        if (vision == null) return;

        currentTarget = vision.GetFirstValidTarget();

        if (currentTarget != null)
        {
            // stop jalan kl ada target di radius
            mover.PauseMovement();
            
            // shoot kl cooldown udah lewat
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                AttackBase(currentTarget);
                lastAttackTime = Time.time;
            }
        }
        else
        {
            // jlan terus selama base g ada di radius
            mover.ResumeMovement();
        }
    }

    private void AttackBase(Transform target)
    {
        Debug.Log($"{gameObject.name} is attacking {target.name} for {attackDamage} damage.");
        
        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(attackDamage);
        }
        else
        {
            target.SendMessageUpwards("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
        }
    }
}