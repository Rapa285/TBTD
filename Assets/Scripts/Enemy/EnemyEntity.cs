using UnityEngine;

[System.Serializable]
public struct EnemyStats
{
    [Min(1f)] public float health;
    [Min(0f)] public float damage;
    [Min(0.1f)] public float movementSpeed;
    [Min(0f)] public float initialShield;
}

[RequireComponent(typeof(HealthComponent))]
public class EnemyEntity : MonoBehaviour
{
    [Header("Stats")]
    public EnemyStats stats = new EnemyStats { health = 100f, damage = 10f, movementSpeed = 3f, initialShield = 0f };
    public EnemyDataSO enemyData;
    [SerializeField, Min(0f)] private float experienceReward = 1f;

    [Header("Targeting")]
    [SerializeField] private Transform baseTarget;

    [Header("Visuals")]
    [SerializeField] private Material enemyMaterial;
    [SerializeField] private MeshRenderer meshRenderer;

    private HealthComponent health;
    private EnemyMover mover;

    public HealthComponent Health => health;
    public EnemyMover Mover => mover;
    public float ExperienceReward => experienceReward;

    public Transform BaseTarget 
    { 
        get { return baseTarget; } 
        set { baseTarget = value; } 
    }

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
        mover = GetComponent<EnemyMover>();

        if (health != null)
        {
            health.OnDeath.AddListener(HandleDeath);
        }

        if (mover != null)
        {
            mover.OnReachEnd.AddListener(HandleReachBase);
        }
    }

    private void Start()
    {
        //Assumed to be initialized externally by EnemySpawner when spawned, but if not, initialize with default stats and base target
        //Initialize(stats, baseTarget);

        if (meshRenderer != null && enemyMaterial != null)
        {
            meshRenderer.material = enemyMaterial;
        }
    }

    public void Initialize()
    {
        if (health != null)
        {
            health.Initialize(stats.health, stats.initialShield);
        }

        if (mover != null)
        {
            mover.Initialize(stats.movementSpeed);
        }
    }
    public void Initialize(EnemyStats newStats, Transform newBaseTarget)
    {
        stats = newStats;
        baseTarget = newBaseTarget;
        Initialize();
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDeath.RemoveListener(HandleDeath);
        }

        if (mover != null)
        {
            mover.OnReachEnd.RemoveListener(HandleReachBase);
        }
    }

    private void OnValidate()
    {
        stats.health = Mathf.Max(1f, stats.health);
        stats.damage = Mathf.Max(0f, stats.damage);
        stats.movementSpeed = Mathf.Max(0.1f, stats.movementSpeed);
        stats.initialShield = Mathf.Max(0f, stats.initialShield);
        experienceReward = Mathf.Max(0f, experienceReward);
    }

    private void HandleReachBase()
    {
        if (baseTarget != null)
        {
            Debug.Log($"{gameObject.name} reached the base and is dealing {stats.damage} damage.");
            CombatDamageUtility.TryApplyDamage(baseTarget, stats.damage);
        }

        GeneralEventBus<BaseDamagedEvent>.Publish(new BaseDamagedEvent
        {
            DamageAmount = stats.damage
        });
        
        PooledObject poolObj = GetComponent<PooledObject>();
        if (poolObj != null)
        {
            poolObj.ReturnToPool();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void HandleDeath()
    {
        if (health != null && health.HasLastHitContext)
        {
            AwardExperience(health.LastHitContext.Attacker, health.LastHitContext.AttackerRoot);
        }

        PooledObject poolObj = GetComponent<PooledObject>();
        if (poolObj != null)
        {
            poolObj.ReturnToPool();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void AwardExperience(TowerEntity attacker, Transform attackerRoot)
    {
        if (experienceReward <= 0f)
        {
            return;
        }

        UnitProgression progression = null;
        if (attacker != null)
        {
            progression = attacker.GetComponentInParent<UnitProgression>();
            if (progression == null)
            {
                progression = attacker.GetComponentInChildren<UnitProgression>(true);
            }
        }

        if (progression == null && attackerRoot != null)
        {
            progression = attackerRoot.GetComponentInParent<UnitProgression>();
            if (progression == null)
            {
                progression = attackerRoot.GetComponentInChildren<UnitProgression>(true);
            }
        }

        if (progression != null)
        {
            progression.AddExperience(experienceReward);
        }
    }
}