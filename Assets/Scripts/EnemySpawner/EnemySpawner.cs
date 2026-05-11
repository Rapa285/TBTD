using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Splines;

public class EnemySpawner : MonoBehaviour
{
    public SplineContainer mapSpline;

    public float gracePeriod = 60;

    // Add enemy types here
    public List<NormalEnemyObject> normalEnemyList = new List<NormalEnemyObject>();
    public List<SpecialEnemyObject> specialEnemyList = new List<SpecialEnemyObject>();

    public float budgetMultiplier = 1;
    public int baseBudget = 10;
    private int budget;

    private int currWave = 1;
    public int waveCount = 3;
    public int waveDuration = 30;

    public List<GameObject> enemyToSpawn = new List<GameObject>();

    private bool isPaused = false;

    [SerializeField]
    private float waveTimer;
    private float spawnInterval;
    private float spawnTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currWave = 1;
        GenerateWave();
    }

    private void Awake()
    {
        SplineContainer spline = GetComponent<SplineContainer>();
        if (spline != null)
        {
            mapSpline = spline;
        }
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
        budget = Mathf.RoundToInt(currWave * budgetMultiplier) + baseBudget; // NOTE: Change Total Cost of each wave here
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
        RaiseNewWaveEvent();
    }

    public void GenerateEnemies()
    {
        if (mapSpline == null) {
            Debug.Log("Missing mapSpline on Spawner!");
            return;
        }        

        List<GameObject> generatedEnemies = new List<GameObject>();

        // Generate Normal Enemies based on budget
        int attempts = 0;
        while(budget > 0 && attempts < 100)
        {
            int randEnemyId = Random.Range(0, normalEnemyList.Count);
            int randEnemyCost = normalEnemyList[randEnemyId].cost;
            int randEnemyMinWave = normalEnemyList[randEnemyId].attribute.minWave;
            int randEnemyMaxWave = normalEnemyList[randEnemyId].attribute.maxWave;

            if (currWave < randEnemyMinWave || currWave > randEnemyMaxWave)
            {
                attempts++;
                continue;
            }

            if (budget - randEnemyCost >= 0)
            {
                GameObject randEnemyObject = normalEnemyList[randEnemyId].enemyPrefab;
                GameObject enemyObject = SetupEnemy(randEnemyObject);

                generatedEnemies.Add(enemyObject);
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

        // Check for Special Enemies 
        // NOTE: Special enemies are spawned last
        foreach (SpecialEnemyObject specialEnemy in specialEnemyList)
        {
            if (currWave == specialEnemy.waveToSpawn)
            {
                GameObject specialEnemyObject = specialEnemy.enemyPrefab;
                GameObject enemyObject = SetupEnemy(specialEnemyObject);

                generatedEnemies.Add(enemyObject);
            }
        }

        enemyToSpawn.Clear();
        enemyToSpawn = generatedEnemies;
    }

    public void SetPauseSpawner(bool isPause)
    {
        isPaused = isPause;
    }

    // Event Bus Stuff
    [SerializeField]
    private WaveEventBus eventBus;

    private void ResolveEventBus()
    {
        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }
    }

    private void RaiseNewWaveEvent()
    {
        ResolveEventBus();
        if (eventBus != null)
        {
            Debug.Log($"Raising NewWaveEvent: Wave {currWave} with {enemyToSpawn.Count} enemies.");
            eventBus.RaiseNewWave(new NewWaveEvent(currWave, enemyToSpawn.Count, enemyToSpawn));
        }
    }

    private GameObject SetupEnemy(GameObject enemyPrefab)
    {
        SplineAnimate animator = enemyPrefab.GetComponent<SplineAnimate>();

        if (mapSpline != null && animator != null)
        {
            // 3. Link them
            animator.Container = mapSpline;
        }
        else
        {
            Debug.LogError("Missing mapSpline on Spawner or SplineAnimate on enemyPrefab!");
        }

        return enemyPrefab;
    }
}

[System.Serializable]
public class NormalEnemyObject
{
    public GameObject enemyPrefab;
    public int cost;
    public ExtraAttr attribute = new ExtraAttr();
}

[System.Serializable]
public class ExtraAttr
{
    public int minWave = 0; 
    public int maxWave = 999;
}

[System.Serializable]
public class SpecialEnemyObject
{
    // Object for Elites/Bosses
    // Special Enemies MUST spawn at certain waves and aren't affected by budget

    public int waveToSpawn = 50;
    public GameObject enemyPrefab;
    // public int cost; #NOTE: Special enemies are not affected by budget
}