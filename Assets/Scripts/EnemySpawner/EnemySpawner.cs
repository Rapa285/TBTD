using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Pool;

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
    private Dictionary<EnemyObject, ObjectPool<PooledObject>> poolDictionary = new Dictionary<EnemyObject, ObjectPool<PooledObject>>();

    public int baseBudget = 10;
    public float budgetMultiplier = 1;
    private int budget;

    [SerializeField]
    private List<EnemyObject> enemyToSpawn = new List<EnemyObject>();
    private List<GameObject> enemyGameObjectCache = new List<GameObject>();

    private bool isInfiniteRound = false;

    private bool isPaused = false;
    private int lastTickSecond = -1;

    private bool hasGracePeriodEnded = false;
    private int lastGraceTickSecond = -1;

    private bool isSpecialWave = false;

    void Start()
    {
        // Setup Spawner Attributes
        currWave = 0;

        foreach (var enemy in normalEnemyList) RegisterEnemyPool(enemy);
        foreach (var enemy in specialEnemyList) RegisterEnemyPool(enemy);
    }

    private void Awake()
    {
        // Setup Spline
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
            HandleGracePeriodTick();
            return;
        }

        if (!hasGracePeriodEnded)
        {
            HandleGracePeriodOver();
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
            SpawnEnemies();
        }
        else
        {
            spawnTimer -= Time.fixedDeltaTime;   
        }
    }

    public void HandleGracePeriodTick()
    {
        gracePeriod -= Time.fixedDeltaTime;
        int currentSecond = Mathf.CeilToInt(gracePeriod);
        if (currentSecond != lastGraceTickSecond && currentSecond >= 0)
        {
            lastGraceTickSecond = currentSecond;
            RaiseGraceTimerTickEvent();
        }
    }

    public void HandleGracePeriodOver()
    {
        hasGracePeriodEnded = true;
        RaiseGraceTimerEndedEvent();
    }

    public void GenerateWave()
    {
        isSpecialWave = false;

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

            if (isSpecialWave) {
                Debug.Log($"Special Wave triggered on Wave {currWave}!");
                RaiseSpecialWaveEvent();
            }
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
                EnemyObject randEnemyObject = normalEnemyList[randEnemyId];

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
                isSpecialWave = true;
                EnemyObject specialEnemyObject = specialEnemyList[i];

                enemyToSpawn.Add(specialEnemyObject);

                Debug.Log($"Added Special Enemy for Wave {currWave}: {specialEnemyObject.objectPrefab.name}");
            }
        }
    }

    private void SetupEnemy(GameObject enemyPrefab)
    {
        enemyPrefab.transform.SetPositionAndRotation(transform.position, transform.rotation);

        // Setup Spline
        SplineAnimate animator = enemyPrefab.GetComponent<SplineAnimate>();

        if (mapSpline != null && animator != null)
        {
            // Link them
            animator.Container = mapSpline;

            animator.Restart(false);
            animator.Play();
        }
        else
        {
            Debug.LogError("Missing mapSpline on Spawner or SplineAnimate on enemyPrefab!");
        }

        enemyPrefab.GetComponent<EnemyEntity>()?.Initialize();
    }

    public void SpawnEnemies()
    {
        if(enemyToSpawn.Count > 0)
        {
            spawnTimer = spawnInterval;
            
            EnemyObject currentEnemyData = enemyToSpawn[0];
            PooledObject enemyInstance = poolDictionary[currentEnemyData].Get();
            SetupEnemy(enemyInstance.gameObject);

            enemyToSpawn.RemoveAt(0);  
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
            
            enemyGameObjectCache.Clear();
            for (int i = 0; i < enemyToSpawn.Count; i++)
            {
                enemyGameObjectCache.Add(enemyToSpawn[i].GetPrefabCopy());
            }

            eventBus.RaiseNewWave(new NewWaveEvent(currWave, enemyToSpawn.Count, enemyGameObjectCache));
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

    private void RaiseSpecialWaveEvent()
    {
        ResolveEventBus();
        if (eventBus != null)
        {
            Debug.Log($"Raising SpecialWaveEvent: Wave {currWave}");

            eventBus.RaiseSpecialWave();
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

    private void RegisterEnemyPool(EnemyObject enemy)
    {
        if (enemy.objectPrefab == null || poolDictionary.ContainsKey(enemy)) return;

        var pool = new ObjectPool<PooledObject>(
            createFunc: () => {
                PooledObject instance = Instantiate(enemy.objectPrefab, this.transform);
                instance.SetPool(poolDictionary[enemy]);
                return instance;
            },
            actionOnGet: (obj) => {
                obj.gameObject.SetActive(true);
                obj.OnGet?.Invoke();
            },
            actionOnRelease: (obj) => {
                obj.gameObject.SetActive(false);
                obj.OnRelease?.Invoke();
            },
            actionOnDestroy: (obj) => Destroy(obj.gameObject)
        );

        poolDictionary.Add(enemy, pool);
    }
}

[System.Serializable]
public class EnemyObject
{
    public PooledObject objectPrefab;
    public GameObject GetPrefabCopy() => objectPrefab.gameObject;
}


[System.Serializable]
public class NormalEnemyObject: EnemyObject
{
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
public class SpecialEnemyObject: EnemyObject
{
    // Object for Elites/Bosses
    // Special Enemies MUST spawn at certain waves and aren't affected by budget

    public int waveToSpawn = 50;
    // public int cost; #NOTE: Special enemies are not affected by budget
}