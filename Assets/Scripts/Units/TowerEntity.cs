using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime authority for tower stats, targeting, attack timing, weapon replacement, and on-hit effects.
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
    private readonly List<OnHitEffectBehaviour> compiledOnHitEffectPrefabs = new List<OnHitEffectBehaviour>();
    private readonly List<OnHitEffectBehaviour> currentOnHitEffectPrefabs = new List<OnHitEffectBehaviour>();
    private readonly List<OnHitEffectBehaviour> activeOnHitEffects = new List<OnHitEffectBehaviour>();
    private AttackBehaviour defaultAttackBehaviour;
    private AttackBehaviour activeAttackBehaviour;
    private AttackBehaviour runtimeReplacementAttackBehaviour;
    private AttackBehaviour currentReplacementPrefab;
    private Transform currentTarget;
    private float nextAttackTime;
    private float activeAfterTime;
    private bool deploymentTimersInitialized;

    public bool Deployed => deployed;
    public AttackBehaviour ActiveAttackBehaviour => GetActiveAttackBehaviour();
    public IReadOnlyList<OnHitEffectBehaviour> ActiveOnHitEffects => activeOnHitEffects;

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

        if (currentTarget == null || !vision.Contains(currentTarget))
        {
            currentTarget = vision.GetFirstValidTarget();
        }

        if (currentTarget == null || Time.time < nextAttackTime)
        {
            return;
        }

        currentAttackBehaviour.Attack(currentTarget, GetStat(ENTITY_STATS.GlobalDamage));
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
    /// Adds one upgrade asset and recompiles runtime stats, weapon, and on-hit effects.
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
        compiledOnHitEffectPrefabs.Clear();

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

        // Weapon replacement is latest-wins, while on-hit effects from all upgrades remain additive.
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

            IReadOnlyList<OnHitEffectBehaviour> onHitEffectPrefabs = upgrade.OnHitEffectPrefabs;
            for (int effectIndex = 0; effectIndex < onHitEffectPrefabs.Count; effectIndex++)
            {
                OnHitEffectBehaviour effectPrefab = onHitEffectPrefabs[effectIndex];
                if (effectPrefab != null)
                {
                    compiledOnHitEffectPrefabs.Add(effectPrefab);
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

        ApplyRuntimeComposition(latestReplacementPrefab, compiledOnHitEffectPrefabs);
    }

    private float GetAttackCooldown()
    {
        return Mathf.Max(0.01f, GetStat(ENTITY_STATS.AttackSpeed));
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
    }

    private void ApplyRuntimeComposition(
        AttackBehaviour replacementPrefab,
        IReadOnlyList<OnHitEffectBehaviour> onHitEffectPrefabs)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ResolveAttackBehaviourReferences();
        UpdateRuntimeReplacement(replacementPrefab);
        UpdateRuntimeOnHitEffects(onHitEffectPrefabs);

        // Preserve the serialized default weapon unless an applied upgrade provides an override.
        activeAttackBehaviour = runtimeReplacementAttackBehaviour != null
            ? runtimeReplacementAttackBehaviour
            : defaultAttackBehaviour;

        if (activeAttackBehaviour != null)
        {
            activeAttackBehaviour.ConfigureRuntime(this, transform, activeOnHitEffects);
        }
    }

    private void ResolveAttackBehaviourReferences()
    {
        if (attackBehaviour == null)
        {
            attackBehaviour = GetComponent<AttackBehaviour>();
        }

        if (defaultAttackBehaviour == null || defaultAttackBehaviour == runtimeReplacementAttackBehaviour)
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

    private void UpdateRuntimeOnHitEffects(IReadOnlyList<OnHitEffectBehaviour> onHitEffectPrefabs)
    {
        if (HasSameOnHitEffectPrefabs(onHitEffectPrefabs) && HasLiveRuntimeOnHitEffects())
        {
            return;
        }

        if (IsCurrentOnHitEffectPrefix(onHitEffectPrefabs))
        {
            // Append-only upgrade flow can add new effect instances without rebuilding existing state.
            for (int i = currentOnHitEffectPrefabs.Count; i < onHitEffectPrefabs.Count; i++)
            {
                AddRuntimeOnHitEffect(onHitEffectPrefabs[i]);
            }

            return;
        }

        DestroyRuntimeOnHitEffects();
        currentOnHitEffectPrefabs.Clear();

        for (int i = 0; i < onHitEffectPrefabs.Count; i++)
        {
            AddRuntimeOnHitEffect(onHitEffectPrefabs[i]);
        }
    }

    private void AddRuntimeOnHitEffect(OnHitEffectBehaviour effectPrefab)
    {
        if (effectPrefab == null)
        {
            return;
        }

        currentOnHitEffectPrefabs.Add(effectPrefab);
        OnHitEffectBehaviour effectInstance = Instantiate(effectPrefab, transform);
        effectInstance.name = $"{effectPrefab.name} (On Hit Runtime)";
        activeOnHitEffects.Add(effectInstance);
    }

    private void DestroyRuntimeOnHitEffects()
    {
        for (int i = 0; i < activeOnHitEffects.Count; i++)
        {
            OnHitEffectBehaviour effect = activeOnHitEffects[i];
            if (effect != null)
            {
                Destroy(effect.gameObject);
            }
        }

        activeOnHitEffects.Clear();
    }

    private bool HasSameOnHitEffectPrefabs(IReadOnlyList<OnHitEffectBehaviour> onHitEffectPrefabs)
    {
        if (currentOnHitEffectPrefabs.Count != onHitEffectPrefabs.Count)
        {
            return false;
        }

        for (int i = 0; i < onHitEffectPrefabs.Count; i++)
        {
            if (currentOnHitEffectPrefabs[i] != onHitEffectPrefabs[i])
            {
                return false;
            }
        }

        return true;
    }

    private bool IsCurrentOnHitEffectPrefix(IReadOnlyList<OnHitEffectBehaviour> onHitEffectPrefabs)
    {
        if (currentOnHitEffectPrefabs.Count > onHitEffectPrefabs.Count)
        {
            return false;
        }

        for (int i = 0; i < currentOnHitEffectPrefabs.Count; i++)
        {
            if (currentOnHitEffectPrefabs[i] != onHitEffectPrefabs[i])
            {
                return false;
            }
        }

        return activeOnHitEffects.Count == currentOnHitEffectPrefabs.Count && HasLiveRuntimeOnHitEffects();
    }

    private bool HasLiveRuntimeOnHitEffects()
    {
        if (activeOnHitEffects.Count != currentOnHitEffectPrefabs.Count)
        {
            return false;
        }

        for (int i = 0; i < activeOnHitEffects.Count; i++)
        {
            if (activeOnHitEffects[i] == null)
            {
                return false;
            }
        }

        return true;
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
