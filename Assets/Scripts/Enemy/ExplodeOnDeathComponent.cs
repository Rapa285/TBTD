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
                Debug.Log($"{gameObject.name} exploded and damaged {hit.name} for {explosionDamage} damage.");
                damageable.TakeDamage(explosionDamage);
            }
            else
            {
                Debug.Log($"{gameObject.name} exploded and hit {hit.name}, but it has no IDamageable component.");
                hit.SendMessageUpwards("TakeDamage", explosionDamage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}