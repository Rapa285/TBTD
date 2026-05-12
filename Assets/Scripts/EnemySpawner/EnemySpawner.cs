using System.Collections.Generic;
using TMPro;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Splines;

public class EnemySpawner : MonoBehaviour
{
    public SplineContainer mapSpline;

    public float gracePeriod = 60;
    private int currWave = 1;
    public int waveCount = 3;
    public int waveDuration = 30;

    [SerializeField]
    private float waveTimer;
    [SerializeField]
    private float spawnInterval;
    [SerializeField]
    private float spawnTimer;

    // Add enemy types here
    public List<NormalEnemyObject> normalEnemyList = new List<NormalEnemyObject>();
    public List<SpecialEnemyObject> specialEnemyList = new List<SpecialEnemyObject>();

    public int baseBudget = 10;
    public float budgetMultiplier = 1;
    private int budget;

    [SerializeField]
    private List<GameObject> enemyToSpawn = new List<GameObject>();

    private bool isInfiniteRound = false;

    private bool isPaused = false;
    private int lastTickSecond = -1;

    private bool hasGracePeriodEnded = false;
    private int lastGraceTickSecond = -1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currWave = 0;
        // GenerateWave();
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
            int currentSecond = Mathf.CeilToInt(gracePeriod);
            if (currentSecond != lastGraceTickSecond && currentSecond >= 0)
            {
                lastGraceTickSecond = currentSecond;
                RaiseGraceTimerTickEvent();
            }
            return;
        }

        if (!hasGracePeriodEnded)
        {
            hasGracePeriodEnded = true;
            RaiseGraceTimerEndedEvent();
        }

        // Pre Infinite Round Handling
        if (currWave <= waveCount)
        {
            if (waveTimer <= 0)
            {
                currWave++;
                if (currWave <= waveCount)
                {
                    GenerateWave();   
                }
                // Currently, infinite round starts after all wave has passed
                else
                {
                    TriggerInfiniteRound();
                }
            }
            else
            {
                waveTimer -= Time.fixedDeltaTime;   
            }

            int currentSecond = Mathf.CeilToInt(waveTimer);
            if (currentSecond != lastTickSecond && currentSecond >= 0)
            {
                lastTickSecond = currentSecond;
                RaiseWaveTimerTickEvent();
            }
        }

        // Infinite Round Handling
        if (isInfiniteRound)
        {
            if (waveTimer <= 0)
            {
                // currWave++; // NOTE: Don't add wave count, because enemy has wave constraint
                GenerateWave();   

                InfiniteRoundAddDifficulty();
            }
            else
            {
                waveTimer -= Time.fixedDeltaTime;   
            }
        }

        if (spawnTimer <= 0)
        {
            if(enemyToSpawn.Count > 0)
            {
                spawnTimer = spawnInterval;

                // TODO change this
                Instantiate(enemyToSpawn[0], transform.position, transform.rotation).SetActive(true);
                enemyToSpawn.RemoveAt(0);   
                
                // Get pool
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
            spawnInterval = (float) waveDuration / enemyToSpawn.Count;    
        }
        else
        {
            spawnInterval = 1f;
        }
        
        waveTimer = waveDuration;
        spawnTimer = 0;

        if (!isInfiniteRound)
        {
            RaiseNewWaveEvent();
        }
    }

    public void GenerateEnemies()
    {
        if (mapSpline == null) {
            Debug.Log("Missing mapSpline on Spawner!");
            return;
        }        

        enemyToSpawn.Clear();

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
                SetupEnemy(randEnemyObject);

                enemyToSpawn.Add(randEnemyObject);
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
        for (int i = 0; i < specialEnemyList.Count; i++)
        {
            if (currWave == specialEnemyList[i].waveToSpawn)
            {
                GameObject specialEnemyObject = specialEnemyList[i].enemyPrefab;
                SetupEnemy(specialEnemyObject);

                enemyToSpawn.Add(specialEnemyObject);
            }
        }

        void SetupEnemy(GameObject enemyPrefab)
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
        }
    }

    public void TriggerInfiniteRound()
    {
        baseBudget *= 2;

        isInfiniteRound = true;
        RaiseInfiniteRoundTriggeredEvent();

        GenerateWave();
    }

    public void InfiniteRoundAddDifficulty()
    {
        baseBudget *= 2;
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

    private void RaiseWaveTimerTickEvent()
    {
        ResolveEventBus();
        if (eventBus != null)
        {
            eventBus.RaiseWaveTimerTick(new WaveTimerTickEvent(waveTimer));
        }
    }

    private void RaiseGraceTimerTickEvent()
    {
        ResolveEventBus();
        if (eventBus != null)
        {
            eventBus.RaiseGraceTimerTick(new GraceTimerTickEvent(gracePeriod));
        }
    }

    private void RaiseGraceTimerEndedEvent()
    {
        ResolveEventBus();
        if (eventBus != null)
        {
            Debug.Log("Raising GraceTimerEndedEvent");
            eventBus.RaiseGraceTimerEnded();
        }
    }

    private void RaiseInfiniteRoundTriggeredEvent()
    {
        ResolveEventBus();
        if (eventBus != null)
        {
            Debug.Log("Raising InfiniteRoundTriggeredEvent");
            eventBus.RaiseInfiniteRoundTriggered();
        }
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