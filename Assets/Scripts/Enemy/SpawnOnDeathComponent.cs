using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class SpawnOnDeathComponent : MonoBehaviour
{
    [SerializeField] private GameObject[] prefabsToSpawn;

    private void Awake()
    {
        GetComponent<HealthComponent>().OnDeath.AddListener(SpawnUnits);
    }

    private void SpawnUnits()
    {
        foreach (var prefab in prefabsToSpawn)
        {
            if (prefab != null)
            {
                // Instantiate unit kecil di posisi yang sama
                Instantiate(prefab, transform.position, Quaternion.identity);
            }
        }
    }
}