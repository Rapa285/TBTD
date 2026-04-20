using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime authority for tower stats, targeting, attack timing, weapon replacement, and upgrade modifiers.
/// </summary>
public class TowerEntity : MonoBehaviour
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

    /// <summary>
    /// Compiles base stats plus applied upgrades into final runtime stats and combat composition.
    /// </summary>
    public void CompileFinalStats()
    {
        finalStats.Clear();
        compiledProjectileModifierPrefabs.Clear();
        compiledAugmentWeaponPrefabs.Clear();

        foreach (ENTITY_STATS stat in Enum.GetValues(typeof(ENTITY_STATS)))
        {
            finalStats[stat] = GetDefaultStat(stat);
        }

        for (int i = 0; i < baseStats.Count; i++)
        {
            finalStats[baseStats[i].stat] = baseStats[i].value;
        }

        // Stat effects are accumulated separately so every final stat uses (base + add) * mult.
        Dictionary<ENTITY_STATS, float> addValues = new Dictionary<ENTITY_STATS, float>();
        Dictionary<ENTITY_STATS, float> multValues = new Dictionary<ENTITY_STATS, float>();

        foreach (ENTITY_STATS stat in Enum.GetValues(typeof(ENTITY_STATS)))
        {
            addValues[stat] = 0f;
            multValues[stat] = 1f;
        }

        // Weapon replacement is latest-wins, while augments and modifiers are additive.
        AttackBehaviour latestReplacementPrefab = null;

        for (int upgradeIndex = 0; upgradeIndex < upgrades.Count; upgradeIndex++)
        {
            UpgradeSO upgrade = upgrades[upgradeIndex];
            if (upgrade == null)
            {
                continue;
            }

            if (upgrade.HasWeaponReplacement)
            {
                latestReplacementPrefab = upgrade.WeaponBehaviourPrefab;
            }
            else if (upgrade.HasWeaponAugment)
            {
                compiledAugmentWeaponPrefabs.Add(upgrade.WeaponBehaviourPrefab);
            }

            IReadOnlyList<ProjectileModifierBehaviour> projectileModifierPrefabs = upgrade.ProjectileModifierPrefabs;
            for (int modifierIndex = 0; modifierIndex < projectileModifierPrefabs.Count; modifierIndex++)
            {
                ProjectileModifierBehaviour modifierPrefab = projectileModifierPrefabs[modifierIndex];
                if (modifierPrefab != null)
                {
                    compiledProjectileModifierPrefabs.Add(modifierPrefab);
                }
            }

            IReadOnlyList<UpgradeSO.StatEffect> statEffects = upgrade.StatEffects;
            for (int effectIndex = 0; effectIndex < statEffects.Count; effectIndex++)
            {
                UpgradeSO.StatEffect effect = statEffects[effectIndex];
                switch (effect.type)
                {
                    case STAT_TYPE.Add:
                        addValues[effect.stat] += effect.value;
                        break;
                    case STAT_TYPE.Mult:
                        multValues[effect.stat] *= effect.value;
                        break;
                }
            }
        }

        foreach (ENTITY_STATS stat in Enum.GetValues(typeof(ENTITY_STATS)))
        {
            finalStats[stat] = (finalStats[stat] + addValues[stat]) * multValues[stat];
        }

        if (vision != null)
        {
            vision.Range = GetStat(ENTITY_STATS.VisualRange);
        }

        ApplyRuntimeComposition(
            latestReplacementPrefab,
            compiledAugmentWeaponPrefabs,
            compiledProjectileModifierPrefabs);
    }

    private float GetAttackCooldown()
    {
        return Mathf.Max(0.01f, GetStat(ENTITY_STATS.AttackSpeed));
    }

    private void AttackWithActiveBehaviours(Transform target, float damageMultiplier)
    {
        AttackBehaviour primaryAttackBehaviour = GetActiveAttackBehaviour();
        if (primaryAttackBehaviour == null)
        {
            return;
        }

        primaryAttackBehaviour.Attack(target, damageMultiplier);

        for (int i = 0; i < runtimeAugmentAttackBehaviours.Count; i++)
        {
            if (!IsAttackTargetStillValid(target))
            {
                break;
            }

            AttackBehaviour augmentAttackBehaviour = runtimeAugmentAttackBehaviours[i];
            if (augmentAttackBehaviour != null)
            {
                augmentAttackBehaviour.Attack(target, damageMultiplier);
            }
        }
    }

    private bool IsAttackTargetStillValid(Transform target)
    {
        return target != null
            && target.gameObject.activeInHierarchy
            && (vision == null || vision.Contains(target));
    }

    private void InitializeDeploymentTimers()
    {
        activeAfterTime = Time.time + GetStat(ENTITY_STATS.SetupTime);
        nextAttackTime = activeAfterTime;
        deploymentTimersInitialized = true;

        if (vision != null)
        {
            vision.Range = GetStat(ENTITY_STATS.VisualRange);
            vision.ScanForTargetsOnce();
        }

        nextEnemyPollTime = Time.time + GetEnemyPollPeriod();
    }

    private void PollEnemiesForDebugIfNeeded()
    {
        if (!activelyPollEnemies || Time.time < nextEnemyPollTime)
        {
            return;
        }

        // Debug polling supports spawned/test enemies that may not enter vision through trigger events.
        vision.ScanForTargetsOnce();
        nextEnemyPollTime = Time.time + GetEnemyPollPeriod();
    }

    private float GetEnemyPollPeriod()
    {
        return Mathf.Max(0.01f, enemyPollPeriod);
    }

    private void ApplyRuntimeComposition(
        AttackBehaviour replacementPrefab,
        IReadOnlyList<AttackBehaviour> augmentWeaponPrefabs,
        IReadOnlyList<ProjectileModifierBehaviour> projectileModifierPrefabs)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ResolveAttackBehaviourReferences();
        UpdateRuntimeReplacement(replacementPrefab);
        UpdateRuntimeAugments(augmentWeaponPrefabs);
        UpdateRuntimeProjectileModifiers(projectileModifierPrefabs);

        // Preserve the serialized default weapon unless an applied upgrade provides an override.
        activeAttackBehaviour = runtimeReplacementAttackBehaviour != null
            ? runtimeReplacementAttackBehaviour
            : defaultAttackBehaviour;

        ConfigureAttackBehaviour(activeAttackBehaviour, projectileModifierPrefabs);
        for (int i = 0; i < runtimeAugmentAttackBehaviours.Count; i++)
        {
            ConfigureAttackBehaviour(runtimeAugmentAttackBehaviours[i], projectileModifierPrefabs);
        }

        RebuildActiveAttackBehaviourList();
    }

    private void ConfigureAttackBehaviour(
        AttackBehaviour behaviour,
        IReadOnlyList<ProjectileModifierBehaviour> projectileModifierPrefabs)
    {
        if (behaviour != null)
        {
            behaviour.ConfigureRuntime(this, transform, activeProjectileModifiers, projectileModifierPrefabs);
        }
    }

    private void ResolveAttackBehaviourReferences()
    {
        if (attackBehaviour == null)
        {
            attackBehaviour = GetComponent<AttackBehaviour>();
        }

        if (defaultAttackBehaviour == null
            || defaultAttackBehaviour == runtimeReplacementAttackBehaviour
            || runtimeAugmentAttackBehaviours.Contains(defaultAttackBehaviour))
        {
            defaultAttackBehaviour = attackBehaviour;
        }

        if (activeAttackBehaviour == null)
        {
            activeAttackBehaviour = defaultAttackBehaviour;
        }
    }

    private void UpdateRuntimeReplacement(AttackBehaviour replacementPrefab)
    {
        if (currentReplacementPrefab == replacementPrefab
            && (replacementPrefab == null || runtimeReplacementAttackBehaviour != null))
        {
            return;
        }

        DestroyRuntimeReplacement();
        currentReplacementPrefab = replacementPrefab;

        if (replacementPrefab == null)
        {
            return;
        }

        // Replacement weapons are instantiated at runtime so upgrade changes never modify prefab-authored defaults.
        runtimeReplacementAttackBehaviour = Instantiate(replacementPrefab, transform);
        runtimeReplacementAttackBehaviour.name = $"{replacementPrefab.name} (Upgrade Runtime)";
    }

    private void DestroyRuntimeReplacement()
    {
        if (runtimeReplacementAttackBehaviour != null)
        {
            Destroy(runtimeReplacementAttackBehaviour.gameObject);
            runtimeReplacementAttackBehaviour = null;
        }
    }

    private void UpdateRuntimeAugments(IReadOnlyList<AttackBehaviour> augmentWeaponPrefabs)
    {
        if (HasSameAugmentWeaponPrefabs(augmentWeaponPrefabs) && HasLiveRuntimeAugments())
        {
            return;
        }

        DestroyRuntimeAugments();
        currentAugmentWeaponPrefabs.Clear();

        for (int i = 0; i < augmentWeaponPrefabs.Count; i++)
        {
            AddRuntimeAugment(augmentWeaponPrefabs[i]);
        }
    }

    private void AddRuntimeAugment(AttackBehaviour augmentPrefab)
    {
        if (augmentPrefab == null)
        {
            return;
        }

        currentAugmentWeaponPrefabs.Add(augmentPrefab);
        AttackBehaviour augmentInstance = Instantiate(augmentPrefab, transform);
        augmentInstance.name = $"{augmentPrefab.name} (Augment Runtime)";
        runtimeAugmentAttackBehaviours.Add(augmentInstance);
    }

    private void DestroyRuntimeAugments()
    {
        for (int i = 0; i < runtimeAugmentAttackBehaviours.Count; i++)
        {
            AttackBehaviour augment = runtimeAugmentAttackBehaviours[i];
            if (augment != null)
            {
                Destroy(augment.gameObject);
            }
        }

        runtimeAugmentAttackBehaviours.Clear();
    }

    private void UpdateRuntimeProjectileModifiers(IReadOnlyList<ProjectileModifierBehaviour> projectileModifierPrefabs)
    {
        if (HasSameProjectileModifierPrefabs(projectileModifierPrefabs) && HasLiveRuntimeProjectileModifiers())
        {
            return;
        }

        if (IsCurrentProjectileModifierPrefix(projectileModifierPrefabs))
        {
            // Append-only upgrade flow can add new modifier instances without rebuilding existing state.
            for (int i = currentProjectileModifierPrefabs.Count; i < projectileModifierPrefabs.Count; i++)
            {
                AddRuntimeProjectileModifier(projectileModifierPrefabs[i]);
            }

            return;
        }

        DestroyRuntimeProjectileModifiers();
        currentProjectileModifierPrefabs.Clear();

        for (int i = 0; i < projectileModifierPrefabs.Count; i++)
        {
            AddRuntimeProjectileModifier(projectileModifierPrefabs[i]);
        }
    }

    private void AddRuntimeProjectileModifier(ProjectileModifierBehaviour modifierPrefab)
    {
        if (modifierPrefab == null)
        {
            return;
        }

        currentProjectileModifierPrefabs.Add(modifierPrefab);
        ProjectileModifierBehaviour modifierInstance = Instantiate(modifierPrefab, transform);
        modifierInstance.name = $"{modifierPrefab.name} (Tower Modifier Runtime)";
        activeProjectileModifiers.Add(modifierInstance);
    }

    private void DestroyRuntimeProjectileModifiers()
    {
        for (int i = 0; i < activeProjectileModifiers.Count; i++)
        {
            ProjectileModifierBehaviour modifier = activeProjectileModifiers[i];
            if (modifier != null)
            {
                Destroy(modifier.gameObject);
            }
        }

        activeProjectileModifiers.Clear();
    }

    private bool HasSameProjectileModifierPrefabs(IReadOnlyList<ProjectileModifierBehaviour> projectileModifierPrefabs)
    {
        return HasSamePrefabList(currentProjectileModifierPrefabs, projectileModifierPrefabs);
    }

    private bool IsCurrentProjectileModifierPrefix(IReadOnlyList<ProjectileModifierBehaviour> projectileModifierPrefabs)
    {
        return IsPrefabListPrefix(currentProjectileModifierPrefabs, projectileModifierPrefabs)
            && HasLiveRuntimeProjectileModifiers();
    }

    private bool HasLiveRuntimeProjectileModifiers()
    {
        return HasLiveRuntimeList(activeProjectileModifiers, currentProjectileModifierPrefabs.Count);
    }

    private bool HasSameAugmentWeaponPrefabs(IReadOnlyList<AttackBehaviour> augmentWeaponPrefabs)
    {
        return HasSamePrefabList(currentAugmentWeaponPrefabs, augmentWeaponPrefabs);
    }

    private bool HasLiveRuntimeAugments()
    {
        return HasLiveRuntimeList(runtimeAugmentAttackBehaviours, currentAugmentWeaponPrefabs.Count);
    }

    private static bool HasSamePrefabList<T>(IReadOnlyList<T> currentPrefabs, IReadOnlyList<T> targetPrefabs)
        where T : UnityEngine.Object
    {
        if (currentPrefabs.Count != targetPrefabs.Count)
        {
            return false;
        }

        for (int i = 0; i < targetPrefabs.Count; i++)
        {
            if (currentPrefabs[i] != targetPrefabs[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsPrefabListPrefix<T>(IReadOnlyList<T> currentPrefabs, IReadOnlyList<T> targetPrefabs)
        where T : UnityEngine.Object
    {
        if (currentPrefabs.Count > targetPrefabs.Count)
        {
            return false;
        }

        for (int i = 0; i < currentPrefabs.Count; i++)
        {
            if (currentPrefabs[i] != targetPrefabs[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasLiveRuntimeList<T>(IReadOnlyList<T> runtimeInstances, int expectedCount)
        where T : UnityEngine.Object
    {
        if (runtimeInstances.Count != expectedCount)
        {
            return false;
        }

        for (int i = 0; i < runtimeInstances.Count; i++)
        {
            if (runtimeInstances[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private void RebuildActiveAttackBehaviourList()
    {
        activeAttackBehaviours.Clear();

        AttackBehaviour primaryAttackBehaviour = GetActiveAttackBehaviour();
        if (primaryAttackBehaviour != null)
        {
            activeAttackBehaviours.Add(primaryAttackBehaviour);
        }

        for (int i = 0; i < runtimeAugmentAttackBehaviours.Count; i++)
        {
            AttackBehaviour augmentAttackBehaviour = runtimeAugmentAttackBehaviours[i];
            if (augmentAttackBehaviour != null)
            {
                activeAttackBehaviours.Add(augmentAttackBehaviour);
            }
        }
    }

    private AttackBehaviour GetActiveAttackBehaviour()
    {
        if (activeAttackBehaviour != null)
        {
            return activeAttackBehaviour;
        }

        if (runtimeReplacementAttackBehaviour != null)
        {
            return runtimeReplacementAttackBehaviour;
        }

        return attackBehaviour;
    }

    private float GetDefaultStat(ENTITY_STATS stat)
    {
        switch (stat)
        {
            case ENTITY_STATS.GlobalDamage:
                return 1f;
            case ENTITY_STATS.AttackSpeed:
                return 1f;
            case ENTITY_STATS.VisualRange:
                return 5f;
            case ENTITY_STATS.SetupTime:
                return 0f;
            default:
                return 0f;
        }
    }
}
