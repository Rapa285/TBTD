using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime authority for tower stats, targeting, attack timing, weapon replacement, and upgrade modifiers.
/// </summary>
public partial class TowerEntity : MonoBehaviour
{
    /// <summary>
    /// Inspector-authored base value for one tower stat.
    /// </summary>
    [Serializable]
    private struct EntityStat
    {
        [Tooltip("Stat represented by this base value.")]
        public ENTITY_STATS stat;

        [Tooltip("Base value used before upgrade modifiers are compiled.")]
        public float value;
    }

    [SerializeField, Tooltip("Vision component used for target discovery and range synchronization.")]
    private UnitVision vision;

    [SerializeField, Tooltip("Default attack behaviour used when no upgrade replaces the weapon.")]
    private AttackBehaviour attackBehaviour;

    [SerializeField, Tooltip("Whether this tower starts active instead of in deployment preview mode.")]
    private bool deployed = true;

    [SerializeField, Tooltip("Debug only: periodically rescan nearby colliders for valid targets while deployed.")]
    private bool activelyPollEnemies;

    [SerializeField, Min(0.01f), Tooltip("Seconds between debug target scans when active polling is enabled.")]
    private float enemyPollPeriod = 0.5f;

    [SerializeField, Tooltip("Base stat values before upgrade modifiers are applied.")]
    private List<EntityStat> baseStats = new List<EntityStat>
    {
        new EntityStat { stat = ENTITY_STATS.GlobalDamage, value = 1f },
        new EntityStat { stat = ENTITY_STATS.AttackSpeed, value = 1f },
        new EntityStat { stat = ENTITY_STATS.VisualRange, value = 5f },
        new EntityStat { stat = ENTITY_STATS.SetupTime, value = 0f }
    };

    [SerializeField, Tooltip("Upgrade assets currently applied to this runtime tower.")]
    private List<UpgradeSO> upgrades = new List<UpgradeSO>();

    private readonly Dictionary<ENTITY_STATS, float> finalStats = new Dictionary<ENTITY_STATS, float>();
    private readonly List<ProjectileModifierBehaviour> compiledProjectileModifierPrefabs = new List<ProjectileModifierBehaviour>();
    private readonly List<AttackBehaviour> compiledAugmentWeaponPrefabs = new List<AttackBehaviour>();
    private readonly List<ProjectileModifierBehaviour> currentProjectileModifierPrefabs = new List<ProjectileModifierBehaviour>();
    private readonly List<AttackBehaviour> currentAugmentWeaponPrefabs = new List<AttackBehaviour>();
    private readonly List<ProjectileModifierBehaviour> activeProjectileModifiers = new List<ProjectileModifierBehaviour>();
    private readonly List<AttackBehaviour> runtimeAugmentAttackBehaviours = new List<AttackBehaviour>();
    private readonly List<AttackBehaviour> activeAttackBehaviours = new List<AttackBehaviour>();
    private AttackBehaviour defaultAttackBehaviour;
    private AttackBehaviour activeAttackBehaviour;
    private AttackBehaviour runtimeReplacementAttackBehaviour;
    private AttackBehaviour currentReplacementPrefab;
    private Transform currentTarget;
    private float nextAttackTime;
    private float activeAfterTime;
    private float nextEnemyPollTime;
    private bool deploymentTimersInitialized;

    public bool Deployed => deployed;
    public AttackBehaviour ActiveAttackBehaviour => GetActiveAttackBehaviour();
    public IReadOnlyList<AttackBehaviour> ActiveAttackBehaviours => activeAttackBehaviours;
    public IReadOnlyList<ProjectileModifierBehaviour> ActiveProjectileModifiers => activeProjectileModifiers;

    private void Awake()
    {
        if (vision == null)
        {
            vision = GetComponentInChildren<UnitVision>();
        }

        if (attackBehaviour == null)
        {
            attackBehaviour = GetComponent<AttackBehaviour>();
        }

        defaultAttackBehaviour = attackBehaviour;
        activeAttackBehaviour = defaultAttackBehaviour;
        RebuildActiveAttackBehaviourList();

        CompileFinalStats();
    }

    /// <summary>
    /// Disables combat while this tower is being positioned as a deployment preview.
    /// </summary>
    public void PrepareForDeploymentPreview()
    {
        deployed = false;
        deploymentTimersInitialized = false;
        currentTarget = null;
        activeAfterTime = float.PositiveInfinity;
        nextAttackTime = float.PositiveInfinity;
        nextEnemyPollTime = float.PositiveInfinity;

        if (vision != null)
        {
            vision.ClearTargets();
        }
    }

    /// <summary>
    /// Activates this tower after placement and initializes attack timing.
    /// </summary>
    public void Deploy()
    {
        deployed = true;
        currentTarget = null;

        if (vision != null)
        {
            vision.ClearTargets();
            vision.Range = GetStat(ENTITY_STATS.VisualRange);
        }

        InitializeDeploymentTimers();
    }

    private void Start()
    {
        if (deployed && !deploymentTimersInitialized)
        {
            InitializeDeploymentTimers();
        }
    }

    private void OnValidate()
    {
        CompileFinalStats();
    }

    private void Update()
    {
        AttackBehaviour currentAttackBehaviour = GetActiveAttackBehaviour();
        if (!deployed || currentAttackBehaviour == null || vision == null || Time.time < activeAfterTime)
        {
            return;
        }

        PollEnemiesForDebugIfNeeded();

        if (currentTarget == null || !vision.Contains(currentTarget))
        {
            currentTarget = vision.GetFirstValidTarget();
        }

        if (currentTarget == null || Time.time < nextAttackTime)
        {
            return;
        }

        AttackWithActiveBehaviours(currentTarget, GetStat(ENTITY_STATS.GlobalDamage));
        nextAttackTime = Time.time + GetAttackCooldown();
    }

    /// <summary>
    /// Reads a compiled final stat value.
    /// </summary>
    public float GetStat(ENTITY_STATS stat)
    {
        if (finalStats.Count == 0)
        {
            CompileFinalStats();
        }

        return finalStats.TryGetValue(stat, out float value) ? value : GetDefaultStat(stat);
    }

    /// <summary>
    /// Sets or adds a base stat value, then recompiles final stats.
    /// </summary>
    public void SetStat(ENTITY_STATS stat, float value)
    {
        for (int i = 0; i < baseStats.Count; i++)
        {
            if (baseStats[i].stat != stat)
            {
                continue;
            }

            EntityStat entityStat = baseStats[i];
            entityStat.value = value;
            baseStats[i] = entityStat;

            CompileFinalStats();
            return;
        }

        baseStats.Add(new EntityStat { stat = stat, value = value });
        CompileFinalStats();
    }

    /// <summary>
    /// Adds one upgrade asset and recompiles runtime stats, weapon, and modifier composition.
    /// </summary>
    public void AddUpgrade(UpgradeSO upgrade)
    {
        if (upgrade == null || upgrades.Contains(upgrade))
        {
            return;
        }

        upgrades.Add(upgrade);
        CompileFinalStats();
    }

    /// <summary>
    /// Removes one upgrade asset and recompiles runtime state. Normal roster flow only adds upgrades.
    /// </summary>
    public void RemoveUpgrade(UpgradeSO upgrade)
    {
        if (!upgrades.Remove(upgrade))
        {
            return;
        }

        CompileFinalStats();
    }
}
