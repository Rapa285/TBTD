using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Pool;

public class EnemySpawner : MonoBehaviour
{
    [Tooltip("Path followed by spawned enemies. If this GameObject has a SplineContainer, Awake assigns it automatically.")]
    public SplineContainer mapSpline;

    [Tooltip("Seconds before the first wave starts. The spawner only raises grace-period timer events during this time.")]
    public float gracePeriod = 60;
    private int currWave = 1;
    [Tooltip("Number of authored waves before the spawner switches to infinite-round generation.")]
    public int waveCount = 3;
    [Tooltip("Seconds allocated for one wave's full spawn queue.")]
    public int waveDuration = 30;
    [Tooltip("Seconds used to spread out enemy spawning within a wave. Clamped to be between 1 and waveDuration.")]
    public int waveSpawnDuration = 30;

    [SerializeField]
    private float waveTimer;
    [SerializeField]
    private float spawnInterval;
    [SerializeField]
    private float spawnTimer;

    // Configure normal enemies here. Each normal enemy consumes wave budget and can be gated by wave range.
    public List<NormalEnemyObject> normalEnemyList = new List<NormalEnemyObject>();
    // Configure boss/elite-style enemies here. Special enemies are injected on exact waves and ignore budget.
    public List<SpecialEnemyObject> specialEnemyList = new List<SpecialEnemyObject>();
    private Dictionary<EnemyObject, ObjectPool<PooledObject>> poolDictionary = new Dictionary<EnemyObject, ObjectPool<PooledObject>>();

    [Tooltip("Base enemy budget added to every generated wave.")]
    public int baseBudget = 10;
    [Tooltip("Wave-scaled budget contribution. Final budget is round(currWave * budgetMultiplier) + baseBudget.")]
    public float budgetMultiplier = 1;
    private int budget;

    // Spawn queue for the current wave. Entries are removed from the front as each enemy is spawned.
    private readonly List<EnemySpawnEntry> enemyToSpawn = new List<EnemySpawnEntry>();
    // Reused list passed to wave UI so it can preview upcoming enemy prefabs without allocating each event.
    private List<GameObject> enemyGameObjectCache = new List<GameObject>();
    // Special waves end only after every special enemy has spawned and every active special enemy is gone.
    private readonly List<TrackedSpecialEnemy> activeSpecialEnemies = new List<TrackedSpecialEnemy>();
    private int pendingSpecialEnemyCount;
    private bool specialWaveActive;

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

    private void OnValidate()
    {
        ClampWaveDurations();
    }

    private void OnDestroy()
    {
        ClearTrackedSpecialEnemies();
    }

    // FixedUpdate keeps wave countdown and spawn cadence tied to Unity's fixed timestep.
    void FixedUpdate()
    {
        if (isPaused)
        {
            return;
        }

        if (gracePeriod > 0)
        {
            // During grace period, do not advance waves or spawn enemies.
            HandleGracePeriodTick();
            return;
        }

        if (!hasGracePeriodEnded)
        {
            HandleGracePeriodOver();
        }

        // Authored wave handling. When all configured waves are consumed, infinite mode takes over.
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

        // Infinite round reuses normal wave generation but keeps currWave fixed for enemy min/max constraints.
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
        // Reset special-wave bookkeeping before building this wave's queue.
        isSpecialWave = false;
        pendingSpecialEnemyCount = 0;
        TryRaiseSpecialWaveEnded();

        budget = Mathf.RoundToInt(currWave * budgetMultiplier) + baseBudget; // NOTE: Change Total Cost of each wave here
        GenerateEnemies();

        if (enemyToSpawn.Count > 0)
        {
            // Spread queued enemies across the spawn window, leaving the rest of waveDuration as downtime.
            int effectiveWaveSpawnDuration = GetEffectiveWaveSpawnDuration();
            spawnInterval = (float) effectiveWaveSpawnDuration / enemyToSpawn.Count;    
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
                specialWaveActive = true;
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

        // Generate normal enemies by spending the wave budget on random valid entries.
        // The attempt cap prevents an infinite loop when the remaining budget cannot buy a valid enemy.
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

                enemyToSpawn.Add(new EnemySpawnEntry(randEnemyObject, false));
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
        // Inject special enemies after budget generation.
        // NOTE: Current insertion appends to the end of the queue because FloorToInt(enemyToSpawn.Count) == Count.
        for (int i = 0; i < specialEnemyList.Count; i++)
        {
            if (currWave == specialEnemyList[i].waveToSpawn)
            {
                isSpecialWave = true;
                EnemyObject specialEnemyObject = specialEnemyList[i];

                enemyToSpawn.Insert(
                    Mathf.FloorToInt(enemyToSpawn.Count),
                    new EnemySpawnEntry(specialEnemyObject, true, specialEnemyList[i].difficultyMultiplier));
                pendingSpecialEnemyCount++;

                Debug.Log($"Added Special Enemy for Wave {currWave}: {specialEnemyObject.objectPrefab.name}");
            }
        }

    }

    private void SetupEnemy(GameObject enemyPrefab)
    {
        // Pooled enemies keep their components; reset transform and spline playback every time they are reused.
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
            
            // ObjectPool returns an inactive/reused instance and actionOnGet activates it before setup.
            EnemySpawnEntry currentEnemyData = enemyToSpawn[0];
            EnemyObject currentEnemy = currentEnemyData.Enemy;
            PooledObject enemyInstance = poolDictionary[currentEnemy].Get();
            SetupEnemy(enemyInstance.gameObject);

            // Setup difficulty multiplier if any
            if (currentEnemyData.DifficultyMultiplier != 1)
            {
                if (enemyInstance.gameObject.TryGetComponent<DifficultyScaler>(out var dscaler))
                {
                    dscaler.ApplyDifficulty(currentEnemyData.DifficultyMultiplier);
                }
            }

            if (currentEnemyData.IsSpecial)
            {
                pendingSpecialEnemyCount = Mathf.Max(0, pendingSpecialEnemyCount - 1);

                // Special enemies use outline while active so warning UI can point players to the threat.
                if (enemyInstance.gameObject.TryGetComponent<Outline>(out var outline))
                {
                    outline.enabled = true;
                }

                RegisterActiveSpecialEnemy(enemyInstance);
            }

            enemyToSpawn.RemoveAt(0);  
            TryRaiseSpecialWaveEnded();
        }
    }

    public void TriggerInfiniteRound()
    {
        // Infinite mode starts with a budget jump, then each generated infinite wave doubles it again.
        baseBudget *= 2;

        isInfiniteRound = true;
        RaiseInfiniteRoundTriggeredEvent();

        GenerateWave();
    }

    public void InfiniteRoundAddDifficulty()
    {
        baseBudget *= 2;
    }

    private int GetEffectiveWaveSpawnDuration()
    {
        ClampWaveDurations();
        return waveSpawnDuration;
    }

    private void ClampWaveDurations()
    {
        waveDuration = Mathf.Max(1, waveDuration);
        waveSpawnDuration = Mathf.Clamp(waveSpawnDuration, 1, waveDuration);
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
                enemyGameObjectCache.Add(enemyToSpawn[i].Enemy.GetPrefabCopy());
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

    private void RaiseSpecialWaveEndedEvent()
    {
        ResolveEventBus();
        if (eventBus != null)
        {
            Debug.Log($"Raising SpecialWaveEndedEvent: Wave {currWave}");

            eventBus.RaiseSpecialWaveEnded();
        }
    }

    private void RegisterActiveSpecialEnemy(PooledObject pooledObject)
    {
        if (pooledObject == null || IsTrackedSpecialEnemy(pooledObject))
        {
            return;
        }

        pooledObject.EnsureEvents();
        HealthComponent health = pooledObject.GetComponent<HealthComponent>();
        Outline outline = pooledObject.GetComponent<Outline>();
        TrackedSpecialEnemy trackedEnemy = new TrackedSpecialEnemy(pooledObject, health, outline);
        activeSpecialEnemies.Add(trackedEnemy);

        // Listen to both death and pool release because pooled enemies may disappear without being killed.
        if (health != null)
        {
            health.OnDeath.AddListener(HandleTrackedSpecialEnemyChanged);
        }

        pooledObject.OnRelease.AddListener(HandleTrackedSpecialEnemyChanged);
    }

    private bool IsTrackedSpecialEnemy(PooledObject pooledObject)
    {
        for (int i = 0; i < activeSpecialEnemies.Count; i++)
        {
            if (activeSpecialEnemies[i].PooledObject == pooledObject)
            {
                return true;
            }
        }

        return false;
    }

    private void HandleTrackedSpecialEnemyChanged()
    {
        PruneInactiveSpecialEnemies();
        TryRaiseSpecialWaveEnded();
    }

    private void PruneInactiveSpecialEnemies()
    {
        for (int i = activeSpecialEnemies.Count - 1; i >= 0; i--)
        {
            TrackedSpecialEnemy trackedEnemy = activeSpecialEnemies[i];
            if (ShouldRemoveTrackedSpecialEnemy(trackedEnemy))
            {
                UnsubscribeTrackedSpecialEnemy(trackedEnemy);
                activeSpecialEnemies.RemoveAt(i);
            }
        }
    }

    private void ClearTrackedSpecialEnemies()
    {
        for (int i = activeSpecialEnemies.Count - 1; i >= 0; i--)
        {
            UnsubscribeTrackedSpecialEnemy(activeSpecialEnemies[i]);
        }

        activeSpecialEnemies.Clear();
    }

    private bool ShouldRemoveTrackedSpecialEnemy(TrackedSpecialEnemy trackedEnemy)
    {
        if (trackedEnemy == null || trackedEnemy.PooledObject == null)
        {
            return true;
        }

        GameObject enemyObject = trackedEnemy.PooledObject.gameObject;
        return enemyObject == null
            || !enemyObject.activeInHierarchy
            || (trackedEnemy.Health != null && trackedEnemy.Health.IsDead);
    }

    private void UnsubscribeTrackedSpecialEnemy(TrackedSpecialEnemy trackedEnemy)
    {
        if (trackedEnemy == null)
        {
            return;
        }

        if (trackedEnemy.Health != null)
        {
            trackedEnemy.Health.OnDeath.RemoveListener(HandleTrackedSpecialEnemyChanged);
        }

        if (trackedEnemy.PooledObject != null)
        {
            trackedEnemy.PooledObject.OnRelease.RemoveListener(HandleTrackedSpecialEnemyChanged);
        }
    }

    private void TryRaiseSpecialWaveEnded()
    {
        if (!specialWaveActive)
        {
            return;
        }

        // Do not end the warning while special enemies are still waiting in the queue or alive in the scene.
        PruneInactiveSpecialEnemies();
        if (pendingSpecialEnemyCount > 0 || activeSpecialEnemies.Count > 0)
        {
            return;
        }

        specialWaveActive = false;
        RaiseSpecialWaveEndedEvent();
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

        // Pools are keyed by the EnemyObject data entry, so each configured enemy prefab gets its own pool.
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

internal readonly struct EnemySpawnEntry
{
    public readonly EnemyObject Enemy;
    public readonly bool IsSpecial;

    public readonly float DifficultyMultiplier;

    public EnemySpawnEntry(EnemyObject enemy, bool isSpecial, float difficultyMultiplier = 1)
    {
        Enemy = enemy;
        IsSpecial = isSpecial;
        DifficultyMultiplier = difficultyMultiplier;
    }
}

internal sealed class TrackedSpecialEnemy
{
    public readonly PooledObject PooledObject;
    public readonly HealthComponent Health;

    // Outline of tracked enemy
    public readonly Outline Outline;

    public TrackedSpecialEnemy(PooledObject pooledObject, HealthComponent health, Outline outline = null)
    {
        PooledObject = pooledObject;
        Health = health;
        Outline = outline;
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
    
    // Difficulty modifier applied to special enemies to make them elites
    public float difficultyMultiplier = 1;
}
