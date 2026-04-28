using UnityEngine;

public class AuraShieldBuffComponent : MonoBehaviour
{
    [Header("Aura Area")]
    [SerializeField] private float auraRadius = 3f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Buff Stats")]
    [SerializeField] private float shieldAmount = 50f;
    [SerializeField] private float buffDuration = 2f;
    [SerializeField] private float buffCooldown = 4f;
    
    private float lastBuffTime = -999f;
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

        foreach (var hit in hits)
        {
            HealthComponent health = hit.GetComponent<HealthComponent>();
            
            if (health != null && hit.gameObject != this.gameObject)
            {
                health.ApplyTemporaryShieldBuff(shieldAmount, buffDuration);
                buffedSomeone = true;
            }
        }

        if (buffedSomeone)
        {
            Debug.Log($"{gameObject.name} casted a shield buff pulse!");
        }
    }
}