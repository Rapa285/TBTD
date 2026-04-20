using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class EnemySpawner : MonoBehaviour
{
    public SplineContainer mapSpline;

    public float gracePeriod = 60;

    // Add enemy types here
    public List<EnemySpawnObject> enemyList = new List<EnemySpawnObject>();

    public int budgetMultiplier;
    private int budget;

    private int currWave = 1;
    public int waveCount = 3;
    public int waveDuration = 30;

    public List<GameObject> enemyToSpawn = new List<GameObject>();

    private bool isPaused = false;

    private float waveTimer;
    private float spawnInterval;
    private float spawnTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currWave = 1;
        GenerateWave();
    }

    // FixedUpdate for consistency
    void FixedUpdate()
    {
        if (isPaused)
        {
            return;
        }

        if (gracePeriod > 0)
        {
            gracePeriod -= Time.fixedDeltaTime;
            return;
        }

        if (waveTimer <= 0 && currWave < waveCount)
        {
            currWave++;
            GenerateWave();
        }      
        else if (currWave < waveCount)
        {
            waveTimer -= Time.fixedDeltaTime;
        }

        if (spawnTimer <= 0)
        {
            if(enemyToSpawn.Count > 0)
            {
                spawnTimer = spawnInterval;

                Instantiate(enemyToSpawn[0], transform.position, transform.rotation).SetActive(true);
                enemyToSpawn.RemoveAt(0);   
            }
        }
        else
        {
            spawnTimer -= Time.fixedDeltaTime;   
        }
    }

    public void GenerateWave()
    {
        budget = currWave * budgetMultiplier; // NOTE: Change Total Cost of each wave here
        GenerateEnemies();

        if (enemyToSpawn.Count > 0)
        {
            spawnInterval = waveDuration / enemyToSpawn.Count;    
        }
        else
        {
            spawnInterval = 1f;
        }
        
        waveTimer = waveDuration;
        spawnTimer = 0;
    }

    public void GenerateEnemies()
    {
        if (mapSpline == null) {
            Debug.Log("Missing mapSpline on Spawner!");
            return;
        }

        

        List<GameObject> generatedEnemies = new List<GameObject>();

        int attempts = 0;
        while(budget > 0 && attempts < 100)
        {
            int randEnemyId = Random.Range(0, enemyList.Count);
            int randEnemyCost = enemyList[randEnemyId].cost;

            if (budget - randEnemyCost >= 0)
            {
                GameObject randEnemyObject = enemyList[randEnemyId].enemyPrefab;
                SplineAnimate animator = randEnemyObject.GetComponent<SplineAnimate>();

                if (mapSpline != null && animator != null)
                {
                    // 3. Link them
                    animator.Container = mapSpline;
                }
                else
                {
                    Debug.LogError("Missing mapSpline on Spawner or SplineAnimate on enemyPrefab!");
                }

                generatedEnemies.Add(enemyList[randEnemyId].enemyPrefab);
                budget -= randEnemyCost;
            }
            else if (budget <= 0)
            {
                break;
            }
            else 
            {
                // TODO: Get cheapest one
                // or break if near zero
                attempts++;
            }
        }

        enemyToSpawn.Clear();
        enemyToSpawn = generatedEnemies;
    }

    public void SetPauseSpawner(bool isPause)
    {
        isPaused = isPause;
    }
}

[System.Serializable]
public class EnemySpawnObject
{
    public GameObject enemyPrefab;
    public int cost;
}