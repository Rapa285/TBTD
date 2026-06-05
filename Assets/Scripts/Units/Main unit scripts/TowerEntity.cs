using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Runtime authority for tower stats, targeting, attack timing, weapon replacement, and upgrade modifiers.
/// </summary>
public partial class TowerEntity : MonoBehaviour
{
    [Serializable]
    public sealed class TowerDeploymentEvent : UnityEvent<string>
    {
    }

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

    [SerializeField, Min(0.01f), Tooltip("Seconds between priority target refreshes while targets are tracked.")]
    private float targetRefreshPeriod = 0.25f;

    [SerializeField, Tooltip("Base stat values before upgrade modifiers are applied.")]
    private List<EntityStat> baseStats = new List<EntityStat>
    {
        new EntityStat { stat = ENTITY_STATS.GlobalDamage, value = 40f },
        new EntityStat { stat = ENTITY_STATS.AttackSpeed, value = 1f },
        new EntityStat { stat = ENTITY_STATS.VisualRange, value = 5f },
        new EntityStat { stat = ENTITY_STATS.SetupTime, value = 1.5f },
        new EntityStat { stat = ENTITY_STATS.AmmoEffectiveness, value = 1f },
        new EntityStat { stat = ENTITY_STATS.AmmoUnits, value = 40f },
        new EntityStat { stat = ENTITY_STATS.DeploymentCooldown, value = 10f },
        new EntityStat { stat = ENTITY_STATS.DeploymentCost, value = 100f },
        new EntityStat { stat = ENTITY_STATS.BulletSize, value = 1f }
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
    private float nextTargetRefreshTime;
    private bool deploymentTimersInitialized;
    private bool deploymentBroadcasted;
    private bool setupCompleteBroadcasted;
    private bool targetSelectionDirty;
    private bool hadValidTargets;
    private bool isSelected;
    private bool isDeploymentPreviewRangeVisible;
    private UnitVision subscribedVision;
    private readonly HashSet<UnityEngine.Object> rangeHoverRequesters = new HashSet<UnityEngine.Object>();

    public bool Deployed => deployed;
    public bool IsSelected => isSelected;
    public UnitVision Vision => vision;
    public AttackBehaviour ActiveAttackBehaviour => GetActiveAttackBehaviour();
    public IReadOnlyList<AttackBehaviour> ActiveAttackBehaviours => activeAttackBehaviours;
    public IReadOnlyList<ProjectileModifierBehaviour> ActiveProjectileModifiers => activeProjectileModifiers;
    public string UnitId => unitId;

    [SerializeField, Tooltip("Broadcast after this tower becomes active and has resolved its runtime unit ID.")]
    private TowerDeploymentEvent onDeploy = new TowerDeploymentEvent();

    public TowerDeploymentEvent OnDeploy => onDeploy;
    public event Action Selected;
    public event Action Deselected;

    private void Awake()
    {
        CacheComponentReferences();
    }

    /// <summary>
    /// Disables combat while this tower is being positioned as a deployment preview.
    /// </summary>
    public void PrepareForDeploymentPreview()
    {
        deployed = false;
        deploymentBroadcasted = false;
        ClearDeploymentRuntimeState();
        ResetAmmoStateForPreview();
        ReleaseResolvedUnitIdForPreview();
        ResolveTowerCoreState();
        SetSelected(false);
        SetDeploymentPreviewRangeVisible(true);
    }

    /// <summary>
    /// Activates this tower after placement and initializes attack timing.
    /// </summary>
    public void Deploy()
    {
        deployed = true;
        SetDeploymentPreviewRangeVisible(false);
        RunDeploymentActivation();
    }

    public void SetSelected(bool selected)
    {
        if (isSelected == selected)
        {
            return;
        }

        isSelected = selected;
        RefreshRangeVisualization();

        if (isSelected)
        {
            Selected?.Invoke();
        }
        else
        {
            Deselected?.Invoke();
        }
    }

    /// <summary>
    /// Shows this tower's range while at least one requester is hovering a linked UI element.
    /// </summary>
    public void SetRangeHoverVisible(UnityEngine.Object requester, bool visible)
    {
        if (requester == null)
        {
            return;
        }

        bool changed = visible
            ? rangeHoverRequesters.Add(requester)
            : rangeHoverRequesters.Remove(requester);

        if (changed)
        {
            RefreshRangeVisualization();
        }
    }

    private void BroadcastDeployment()
    {
        if (!deployed || deploymentBroadcasted || string.IsNullOrWhiteSpace(unitId))
        {
            return;
        }

        deploymentBroadcasted = true;
        onDeploy?.Invoke(unitId);

        UnitEventBus resolvedEventBus = ResolveEventBus();
        if (resolvedEventBus != null)
        {
            resolvedEventBus.RaiseTowerDeployed(new TowerDeployedEvent(unitId, this));
        }
    }

    private void Start()
    {
        if (deployed)
        {
            RunDeploymentActivation();
            return;
        }

        ResolveTowerCoreState();
    }

    private void OnValidate()
    {
        CacheComponentReferences();
        ResolveTowerCoreState();
    }

    private void Update()
    {
        if (!deployed)
        {
            return;
        }

        BroadcastSetupCompletedIfReady();

        AttackBehaviour currentAttackBehaviour = GetActiveAttackBehaviour();
        if (currentAttackBehaviour == null || vision == null || Time.time < activeAfterTime)
        {
            return;
        }

        PollEnemiesForDebugIfNeeded();
        UpdateTargetSelectionIfNeeded();

        if (currentTarget == null || Time.time < nextAttackTime)
        {
            return;
        }

        if (!CanStartPrimaryAttack(currentAttackBehaviour))
        {
            return;
        }

        AttackWithActiveBehaviours(currentTarget, GetStat(ENTITY_STATS.GlobalDamage));
        nextAttackTime = Time.time + GetAttackCooldown();
        if (vision != null && vision.HasValidTargets)
        {
            NotifyActiveAttackBehavioursTargetsAvailable();
        }
    }

    /// <summary>
    /// Reads a compiled final stat value.
    /// </summary>
    public float GetStat(ENTITY_STATS stat)
    {
        if (finalStats.Count == 0)
        {
            ResolveTowerCoreState();
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

            RefreshTowerStateForCurrentMode();
            return;
        }

        baseStats.Add(new EntityStat { stat = stat, value = value });
        RefreshTowerStateForCurrentMode();
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
        RefreshTowerStateForCurrentMode();
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

        RefreshTowerStateForCurrentMode();
    }

    /// <summary>
    /// Replaces one resolved upgrade asset with another and refreshes runtime state once.
    /// </summary>
    public void ReplaceUpgrade(UpgradeSO oldUpgrade, UpgradeSO newUpgrade)
    {
        bool changed = false;

        if (oldUpgrade != null && oldUpgrade != newUpgrade)
        {
            changed = upgrades.Remove(oldUpgrade) || changed;
        }

        if (newUpgrade != null && !upgrades.Contains(newUpgrade))
        {
            upgrades.Add(newUpgrade);
            changed = true;
        }

        if (changed)
        {
            RefreshTowerStateForCurrentMode();
        }
    }

    private void CacheComponentReferences()
    {
        if (vision == null)
        {
            vision = GetComponentInChildren<UnitVision>();
        }

        if (attackBehaviour == null)
        {
            attackBehaviour = GetComponent<AttackBehaviour>();
        }

        if (defaultAttackBehaviour == null)
        {
            defaultAttackBehaviour = attackBehaviour;
        }

        if (activeAttackBehaviour == null)
        {
            activeAttackBehaviour = defaultAttackBehaviour;
        }

        if (activeAttackBehaviours.Count == 0)
        {
            RebuildActiveAttackBehaviourList();
        }

        if (onDeploy == null)
        {
            onDeploy = new TowerDeploymentEvent();
        }

        SubscribeToVisionEventsIfNeeded();
    }

    private void RefreshTowerStateForCurrentMode()
    {
        if (!Application.isPlaying)
        {
            ResolveTowerCoreState();
            return;
        }

        if (deployed && deploymentBroadcasted)
        {
            RunDeployedRuntimeRefresh();
            return;
        }

        ResolveTowerCoreState();
    }

    private void RunDeploymentActivation()
    {
        // Deployment activation is intentionally linear: identity, tower core, dependent runtime state, timers, broadcast.
        if (!ValidateOrResolveUnitId())
        {
            return;
        }

        ResolveTowerCoreState();
        ResolvePrimaryRuntimeFeatures(true);
        InitializeDeploymentRuntime();
        BroadcastDeployment();
    }

    private void RunDeployedRuntimeRefresh()
    {
        // Live refresh keeps the same order as deployment, but finishes with a runtime-modified event instead of OnDeploy.
        if (!ValidateOrResolveUnitId())
        {
            return;
        }

        ResolveTowerCoreState();
        ResolvePrimaryRuntimeFeatures(false);
        RefreshDeploymentRuntime();
        BroadcastTowerModified();
    }

    private void ResolveTowerCoreState()
    {
        CacheComponentReferences();
        CompileFinalStats();
    }

    private void SetDeploymentPreviewRangeVisible(bool visible)
    {
        if (isDeploymentPreviewRangeVisible == visible)
        {
            return;
        }

        isDeploymentPreviewRangeVisible = visible;
        RefreshRangeVisualization();
    }

    private void RefreshRangeVisualization()
    {
        if (vision == null)
        {
            CacheComponentReferences();
        }

        if (vision != null)
        {
            vision.SetVisualizationVisible(isSelected || isDeploymentPreviewRangeVisible || HasRangeHoverRequesters());
        }
    }

    private bool HasRangeHoverRequesters()
    {
        if (rangeHoverRequesters.Count == 0)
        {
            return false;
        }

        rangeHoverRequesters.RemoveWhere(requester => requester == null);
        return rangeHoverRequesters.Count > 0;
    }

    private void ResolvePrimaryRuntimeFeatures(bool isInitialActivation)
    {
        if (isInitialActivation)
        {
            InitializeAmmoState();
            return;
        }

        RefreshAmmoCapacityFromStats();
    }

    private void BroadcastTowerModified()
    {
        if (!deployed || string.IsNullOrWhiteSpace(unitId))
        {
            return;
        }

        UnitEventBus resolvedEventBus = ResolveEventBus();
        if (resolvedEventBus == null)
        {
            return;
        }

        resolvedEventBus.RaiseTowerModified(new TowerModifiedEvent(unitId, this));
    }
}
