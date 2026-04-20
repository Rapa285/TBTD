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
            Transform target = ColliderTargetUtility.GetTargetTransform(hit);
            if (target == null || target == transform || target.IsChildOf(transform))
            {
                continue;
            }

            Debug.Log($"{gameObject.name} exploded and damaged {target.name} for {explosionDamage} damage.");
            CombatDamageUtility.TryApplyDamage(target, explosionDamage);
        }
    }
}
