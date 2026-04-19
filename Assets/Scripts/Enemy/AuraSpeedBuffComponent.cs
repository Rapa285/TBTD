using UnityEngine;

public class AuraSpeedBuffComponent : MonoBehaviour
{
    [SerializeField] private float auraRadius = 3f;
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private LayerMask enemyLayer;

    private void Update()
    {
        // Mencari musuh di sekitar setiap frame
        Collider[] hits = Physics.OverlapSphere(transform.position, auraRadius, enemyLayer);
        foreach (var hit in hits)
        {
            EnemyMover mover = hit.GetComponent<EnemyMover>();
            // gbisa buff diri sendiri
            if (mover != null && hit.gameObject != this.gameObject)
            {
                mover.ApplyTemporarySpeedBuff(speedMultiplier, 0.2f);
            }
        }
    }
}