using UnityEngine;

public class AuraSpeedBuffComponent : MonoBehaviour, IDifficultyScalable
{
    [SerializeField] private EnemyAudio enemyAudio;
    [Header("Aura Area")]
    [SerializeField] private float auraRadius = 3f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Buff Stats")]
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private float buffDuration = 2f;
    [SerializeField] private float buffCooldown = 4f;
    
    private float lastBuffTime = -999f;
    private VFXService vfxService;

    private void ResolveService()
    {
        if (vfxService == null)
        {
            ServiceLocator.TryResolve(out vfxService);
        }
    }
    private void Update()
    {
        if (Time.time >= lastBuffTime + buffCooldown)
        {
            CastBuffPulse();
            lastBuffTime = Time.time;
        }
    }

    private void CastBuffPulse()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, auraRadius, enemyLayer);
        bool buffedSomeone = false;
        int buffedTargetCount = 0;

        foreach (var hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            EnemyMover mover = hit.GetComponentInParent<EnemyMover>();
            
            if (mover != null && mover.gameObject != this.gameObject)
            {
                mover.ApplyTemporarySpeedBuff(speedMultiplier, buffDuration);
                buffedSomeone = true;
                buffedTargetCount++;
            }
        }

        if (buffedSomeone)
        {
            ResolveService();
            if (enemyAudio != null)
            {
                enemyAudio.PlaySkill();
            }
            Debug.Log($"{gameObject.name} casted a speed buff pulse and buffed {buffedTargetCount} target(s).");

            Debug.Log($"{gameObject.name} requesting {nameof(VFXType.BuffAura)} VFX.");
            if (vfxService != null)
            {
                vfxService.HandleRequest(VFXType.BuffAura, transform, transform, follow: true);
            }
            else
            {
                Debug.LogWarning($"{gameObject.name} could not request {nameof(VFXType.BuffAura)} VFX because no {nameof(VFXService)} was resolved.", this);
            }
        }
        else
        {
            Debug.Log($"{gameObject.name} cast buff pulse but found no valid buff targets.");
        }
    }

    public void ScaleDifficulty(float multiplier)
    {
        speedMultiplier = 1f + ((speedMultiplier - 1f) * multiplier); 
        
        buffCooldown = Mathf.Max(1.5f, buffCooldown / (1f + ((multiplier - 1f) * 0.2f)));
    }
}