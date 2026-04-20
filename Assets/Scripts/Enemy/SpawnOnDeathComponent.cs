using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(HealthComponent), typeof(EnemyEntity))]
public class SpawnOnDeathComponent : MonoBehaviour
{
    [SerializeField] private GameObject[] prefabsToSpawn;
    
    [Header("Spawn Settings")]
    [Tooltip("Unit spawn spacing")]
    [SerializeField] private float pathSpacing = 0.02f; 

    private EnemyEntity parentEntity;
    private SplineAnimate parentSpline;

    private void Awake()
    {
        parentEntity = GetComponent<EnemyEntity>();
        parentSpline = GetComponent<SplineAnimate>();

        GetComponent<HealthComponent>().OnDeath.AddListener(SpawnUnits);
    }

    private void SpawnUnits()
    {
        float baseNormalizedTime = parentSpline != null ? parentSpline.NormalizedTime : 0f;

        for (int i = 0; i < prefabsToSpawn.Length; i++)
        {
            GameObject prefab = prefabsToSpawn[i];
            if (prefab != null)
            {
                GameObject spawnedUnit = Instantiate(prefab, transform.position, transform.rotation);
                
                // get from parent
                EnemyEntity childEntity = spawnedUnit.GetComponent<EnemyEntity>();
                if (childEntity != null && parentEntity != null)
                {
                    childEntity.BaseTarget = parentEntity.BaseTarget;
                }

                SplineAnimate childSpline = spawnedUnit.GetComponent<SplineAnimate>();
                if (childSpline != null && parentSpline != null)
                {
                    childSpline.Container = parentSpline.Container;
                    
                    float offsetTime = Mathf.Max(0f, baseNormalizedTime - (i * pathSpacing));
                    childSpline.StartCoroutine(ForcePositionNextFrame(childSpline, offsetTime));
                }
            }
        }
    }

    private IEnumerator ForcePositionNextFrame(SplineAnimate spline, float timeToSet)
    {
        yield return null;

        if (spline != null)
        {
            spline.NormalizedTime = timeToSet;
            spline.Play();
        }
    }
}
