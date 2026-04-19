using UnityEngine;

public class AuraSpeedBuffComponent : MonoBehaviour
{
    [Header("Aura Area")]
    [SerializeField] private float auraRadius = 3f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Buff Stats")]
    [SerializeField] private float speedMultiplier = 1.5f;
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
            EnemyMover mover = hit.GetComponent<EnemyMover>();
            
            if (mover != null && hit.gameObject != this.gameObject)
            {
                mover.ApplyTemporarySpeedBuff(speedMultiplier, buffDuration);
                buffedSomeone = true;
            }
        }

        if (buffedSomeone)
        {
            Debug.Log($"{gameObject.name} casted a speed buff pulse!");
        }
    }
}