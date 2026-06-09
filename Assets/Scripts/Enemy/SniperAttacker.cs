using UnityEngine;

[RequireComponent(typeof(EnemyMover))]
public class SniperAttacker : MonoBehaviour, IDifficultyScalable
{
    [Header("Sniper Stats")]
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float baseAttackCooldown = 2f;
    
    [Header("Targeting")]
    [SerializeField] private UnitVision vision;
    [SerializeField] private EnemyAudio enemyAudio;

    [Header("VFX Settings")]
    [SerializeField] private Transform firePoint; 

    private EnemyMover mover;
    private float lastAttackTime;
    private Transform currentTarget;

    private void Awake()
    {
        mover = GetComponent<EnemyMover>();
        if (vision == null) vision = GetComponentInChildren<UnitVision>();
    }

    private void Update()
    {
        if (vision == null) return;

        currentTarget = vision.GetFirstValidTarget();

        if (currentTarget != null)
        {
            if (mover != null) mover.PauseMovement();
            
            float currentMultiplier = mover != null ? mover.CurrentSpeedMultiplier : 1f;
            float actualCooldown = baseAttackCooldown / currentMultiplier; 
            // Jika di-buff 2x lebih cepat, cooldown dibagi 2 (semakin cepat nembak)
            
            if (Time.time >= lastAttackTime + actualCooldown)
            {
                AttackBase(currentTarget);
                lastAttackTime = Time.time;
            }
        }
        else
        {
            if (mover != null) mover.ResumeMovement();
        }
    }

    private void AttackBase(Transform target)
    {
        CombatDamageUtility.TryApplyDamage(target, attackDamage);
        
        if (enemyAudio != null) enemyAudio.PlayAttackBase(); 

        if (firePoint != null && target != null)
        {
            SniperTracerVFX.GlobalStartPos = firePoint.position;
            SniperTracerVFX.GlobalEndPos = target.position;

            if (ServiceLocator.TryResolve<VFXService>(out VFXService vfxService))
            {
                vfxService.HandleRequest(VFXType.LaserHit, firePoint, null, false);
            }
        }
    }

    public void ScaleDifficulty(float multiplier)
    {
        attackDamage *= multiplier;        

        baseAttackCooldown = Mathf.Max(0.5f, baseAttackCooldown / (1f + ((multiplier - 1f) * 0.2f)));
    }
}