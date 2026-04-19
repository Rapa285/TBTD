using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class ExplodeOnDeathComponent : MonoBehaviour
{
    [SerializeField] private float explosionRadius = 2.5f;
    [SerializeField] private float explosionDamage = 25f;
    [SerializeField] private LayerMask targetLayer;

    private void Awake()
    {
        GetComponent<HealthComponent>().OnDeath.AddListener(Explode);
    }

    private void Explode()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, targetLayer);
        foreach (var hit in hits)
        {
            // Mencoba memberikan damage ke menara/objek di sekitar
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(explosionDamage);
            }
            else
            {
                hit.SendMessageUpwards("TakeDamage", explosionDamage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}