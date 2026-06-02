using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent roster state for all player-owned units.
/// </summary>
[DefaultExecutionOrder(-900)]
public class UnitStateManager : MonoBehaviour
{
    /// <summary>
    /// Persistent selected level for one multi-upgrade line.
    /// </summary>
    [Serializable]
    public sealed class AppliedMultiUpgradeState
    {
        [SerializeField, Tooltip("Selected multi-upgrade line.")]
        private MultiUpgradeSO multiUpgrade;

        [SerializeField, Min(0), Tooltip("Current selected level in this multi-upgrade line. Zero means inactive.")]
        private int level;

        public MultiUpgradeSO MultiUpgrade => multiUpgrade;
        public int Level => Mathf.Max(0, level);
        public int MaxLevel => multiUpgrade != null ? multiUpgrade.MaxLevel : 0;

        public AppliedMultiUpgradeState()
        {
        }

        public AppliedMultiUpgradeState(MultiUpgradeSO multiUpgrade, int level)
        {
            SetMultiUpgrade(multiUpgrade);
            SetLevel(level);
        }

        public bool TryGetActiveUpgrade(out UpgradeSO upgrade)
        {
            if (multiUpgrade != null && Level > 0)
            {
                return multiUpgrade.TryGetLevelUpgrade(Level, out upgrade);
            }

            upgrade = null;
            return false;
        }

        internal void SetMultiUpgrade(MultiUpgradeSO value)
        {
            multiUpgrade = value;
        }

        internal void SetLevel(int value)
        {
            level = Mathf.Clamp(value, 0, MaxLevel);
        }
    }

    /// <summary>
    /// Serialized persistent state for one owned unit plus transient runtime bindings.
    /// </summary>
    [Serializable]
    public sealed class OwnedUnitState
    {
        [SerializeField, Tooltip("Stable roster ID used by UI, deployment, progression, and upgrades.")]
        private string unitId;

        [SerializeField, Tooltip("Display name for UI that presents this owned unit.")]
        private string displayName;

        [SerializeField, Tooltip("Optional UI icon for this owned unit.")]
        private Sprite icon;

        [SerializeField, Tooltip("Plain tower prefab deployed for this owned unit.")]
        private TowerEntity unitPrefab;

        [SerializeField, Tooltip("Current persistent level for this owned unit.")]
        private int level = 1;

        [SerializeField, Tooltip("Current persistent XP stored for this owned unit.")]
        private float experience;

        [SerializeField, Tooltip("Whether this unit is waiting for an upgrade selection.")]
        private bool upgradePending;

        [SerializeField, Tooltip("Selected multi-upgrade lines for this owned unit. Runtime stat and weapon composition still happens inside TowerEntity.")]
        private List<AppliedMultiUpgradeState> appliedMultiUpgrades = new List<AppliedMultiUpgradeState>();

        [SerializeField, Tooltip("Most recently selected multi-upgrade line, used for roster UI tie-breaking.")]
        private MultiUpgradeSO latestSelectedMultiUpgrade;

        [SerializeField, Tooltip("Selected weapon evolution for this owned unit. Only one evolution can be active.")]
        private EvolutionSO selectedEvolution;

        [NonSerialized] private List<UpgradeSO> resolvedAppliedUpgrades;
        [NonSerialized] private TowerEntity currentRuntimeInstance;
        [NonSerialized] private GameObject currentRuntimeRoot;
        [NonSerialized] private float cooldownDuration;
        [NonSerialized] private float cooldownEndTime;
        [NonSerialized] private bool cooldownActive;
        [NonSerialized] private bool hasCompiledDeploymentCost;
        [NonSerialized] private int deploymentCost;

        public string UnitId => unitId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public TowerEntity UnitPrefab => unitPrefab;
        public int Level => Mathf.Max(1, level);
        public float Experience => Mathf.Max(0f, experience);
        public bool UpgradePending => upgradePending;
        public IReadOnlyList<AppliedMultiUpgradeState> AppliedMultiUpgrades => appliedMultiUpgrades;
        public MultiUpgradeSO LatestSelectedMultiUpgrade => latestSelectedMultiUpgrade;
        public EvolutionSO SelectedEvolution => selectedEvolution;
        public bool HasSelectedEvolution => selectedEvolution != null;
        public IReadOnlyList<UpgradeSO> AppliedUpgrades => GetResolvedAppliedUpgrades();
        public TowerEntity CurrentRuntimeInstance => currentRuntimeInstance;
        public GameObject CurrentRuntimeRoot => currentRuntimeRoot;
        public bool IsDeployed => currentRuntimeInstance != null;
        public bool IsCoolingDown => cooldownActive && CooldownRemaining > 0f;
        public float CooldownDuration => cooldownActive ? cooldownDuration : 0f;
        public float CooldownRemaining => cooldownActive ? Mathf.Max(0f, cooldownEndTime - Time.time) : 0f;
        public float CooldownNormalizedRemaining => cooldownDuration > 0f
            ? Mathf.Clamp01(CooldownRemaining / cooldownDuration)
            : 0f;
        public bool HasCompiledDeploymentCost => hasCompiledDeploymentCost;
        public int DeploymentCost => hasCompiledDeploymentCost ? deploymentCost : 0;

        /// <summary>
        /// Returns whether this unit has the given resolved upgrade active through any multi-upgrade line.
        /// </summary>
        public bool HasAppliedUpgrade(UpgradeSO upgrade)
        {
            if (upgrade == null)
            {
                return false;
            }

            IReadOnlyList<UpgradeSO> resolvedUpgrades = GetResolvedAppliedUpgrades();
            for (int i = 0; i < resolvedUpgrades.Count; i++)
            {
                if (resolvedUpgrades[i] == upgrade)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the currently selected level for one multi-upgrade line.
        /// </summary>
        public int GetAppliedMultiUpgradeLevel(MultiUpgradeSO multiUpgrade)
        {
            AppliedMultiUpgradeState state = FindAppliedMultiUpgradeState(multiUpgrade);
            return state != null ? state.Level : 0;
        }

        /// <summary>
        /// Returns whether this unit has already reached the final configured level for a multi-upgrade line.
        /// </summary>
        public bool HasMaxMultiUpgradeLevel(MultiUpgradeSO multiUpgrade)
        {
            return multiUpgrade != null
                && multiUpgrade.MaxLevel > 0
                && GetAppliedMultiUpgradeLevel(multiUpgrade) >= multiUpgrade.MaxLevel;
        }

        /// <summary>
        /// Resolves the next selectable level for a multi-upgrade line without changing roster state.
        /// </summary>
        public bool TryGetNextMultiUpgradeLevel(
            MultiUpgradeSO multiUpgrade,
            out int currentLevel,
            out int nextLevel,
            out UpgradeSO upgrade)
        {
            currentLevel = GetAppliedMultiUpgradeLevel(multiUpgrade);
            if (multiUpgrade != null)
            {
                return multiUpgrade.TryGetNextLevelUpgrade(currentLevel, out nextLevel, out upgrade);
            }

            nextLevel = 0;
            upgrade = null;
            return false;
        }

        /// <summary>
        /// Returns whether this unit can select the supplied evolution based on current roster state.
        /// </summary>
        public bool CanSelectEvolution(EvolutionSO evolution)
        {
            if (selectedEvolution != null
                || evolution == null
                || !evolution.HasResolvedUpgrade)
            {
                return false;
            }

            IReadOnlyList<EvolutionSO.Prerequisite> prerequisites = evolution.Prerequisites;
            for (int i = 0; i < prerequisites.Count; i++)
            {
                EvolutionSO.Prerequisite prerequisite = prerequisites[i];
                MultiUpgradeSO multiUpgrade = prerequisite.MultiUpgrade;
                if (multiUpgrade == null
                    || GetAppliedMultiUpgradeLevel(multiUpgrade) < prerequisite.MinimumLevel)
                {
                    return false;
                }
            }

            return true;
        }

        internal void SetExperience(float value)
        {
            experience = Mathf.Max(0f, value);
        }

        internal void SetUpgradePending(bool value)
        {
            upgradePending = value;
        }

        internal void AdvanceLevel()
        {
            level = Level + 1;
        }

        internal bool ApplyNextMultiUpgradeLevel(
            MultiUpgradeSO multiUpgrade,
            out UpgradeSO previousUpgrade,
            out UpgradeSO nextUpgrade,
            out int nextLevel)
        {
            previousUpgrade = null;
            nextUpgrade = null;
            nextLevel = 0;

            if (!TryGetNextMultiUpgradeLevel(multiUpgrade, out _, out nextLevel, out nextUpgrade))
            {
                return false;
            }

            AppliedMultiUpgradeState state = FindAppliedMultiUpgradeState(multiUpgrade);
            if (state == null)
            {
                state = new AppliedMultiUpgradeState(multiUpgrade, nextLevel);
                appliedMultiUpgrades.Add(state);
                latestSelectedMultiUpgrade = multiUpgrade;
                InvalidateResolvedAppliedUpgrades();
                return true;
            }

            state.TryGetActiveUpgrade(out previousUpgrade);
            state.SetLevel(nextLevel);
            latestSelectedMultiUpgrade = multiUpgrade;
            InvalidateResolvedAppliedUpgrades();
            return true;
        }

        internal bool TrySelectEvolution(EvolutionSO evolution, out UpgradeSO resolvedUpgrade)
        {
            resolvedUpgrade = null;
            if (!CanSelectEvolution(evolution))
            {
                return false;
            }

            selectedEvolution = evolution;
            resolvedUpgrade = evolution.ResolvedUpgrade;
            InvalidateResolvedAppliedUpgrades();
            return resolvedUpgrade != null;
        }

        internal bool DebugForceMultiUpgradeLevel(MultiUpgradeSO multiUpgrade, int targetLevel)
        {
            if (multiUpgrade == null || targetLevel <= 0)
            {
                return false;
            }

            int currentLevel = GetAppliedMultiUpgradeLevel(multiUpgrade);
            int clampedLevel = Mathf.Clamp(Mathf.Max(currentLevel, targetLevel), 1, multiUpgrade.MaxLevel);
            if (!multiUpgrade.TryGetLevelUpgrade(clampedLevel, out _))
            {
                return false;
            }

            AppliedMultiUpgradeState state = FindAppliedMultiUpgradeState(multiUpgrade);
            if (state == null)
            {
                state = new AppliedMultiUpgradeState(multiUpgrade, clampedLevel);
                appliedMultiUpgrades.Add(state);
            }
            else
            {
                state.SetLevel(clampedLevel);
            }

            latestSelectedMultiUpgrade = multiUpgrade;
            InvalidateResolvedAppliedUpgrades();
            return true;
        }

        internal bool DebugForceEvolution(EvolutionSO evolution)
        {
            if (evolution == null || !evolution.HasResolvedUpgrade)
            {
                return false;
            }

            selectedEvolution = evolution;
            InvalidateResolvedAppliedUpgrades();
            return true;
        }

        internal bool DebugClearUpgrades()
        {
            bool hadUpgrades = (appliedMultiUpgrades != null && appliedMultiUpgrades.Count > 0)
                || selectedEvolution != null;

            appliedMultiUpgrades?.Clear();
            latestSelectedMultiUpgrade = null;
            selectedEvolution = null;
            InvalidateResolvedAppliedUpgrades();
            return hadUpgrades;
        }

        internal List<UpgradeSO> DebugCreateAppliedUpgradeSnapshot()
        {
            return new List<UpgradeSO>(GetResolvedAppliedUpgrades());
        }

        internal void SetRuntimeInstance(TowerEntity tower, GameObject root)
        {
            currentRuntimeInstance = tower;
            currentRuntimeRoot = root != null ? root : tower != null ? tower.gameObject : null;
            ClearCooldown();
        }

        internal void ClearRuntimeInstance()
        {
            currentRuntimeInstance = null;
            currentRuntimeRoot = null;
        }

        internal void StartCooldown(float duration)
        {
            cooldownDuration = Mathf.Max(0f, duration);
            if (cooldownDuration <= 0f)
            {
                ClearCooldown();
                return;
            }

            cooldownEndTime = Time.time + cooldownDuration;
            cooldownActive = true;
        }

        internal bool TryEndExpiredCooldown()
        {
            if (!cooldownActive || CooldownRemaining > 0f)
            {
                return false;
            }

            ClearCooldown();
            return true;
        }

        internal void ClearCooldown()
        {
            cooldownDuration = 0f;
            cooldownEndTime = 0f;
            cooldownActive = false;
        }

        internal void SetCompiledDeploymentCost(int value)
        {
            deploymentCost = Mathf.Max(0, value);
            hasCompiledDeploymentCost = true;
        }

        internal void ClearCompiledDeploymentCost()
        {
            deploymentCost = 0;
            hasCompiledDeploymentCost = false;
        }

        private IReadOnlyList<UpgradeSO> GetResolvedAppliedUpgrades()
        {
            if (resolvedAppliedUpgrades == null)
            {
                resolvedAppliedUpgrades = new List<UpgradeSO>();
            }

            RebuildResolvedAppliedUpgrades();
            return resolvedAppliedUpgrades;
        }

        private void InvalidateResolvedAppliedUpgrades()
        {
            if (resolvedAppliedUpgrades != null)
            {
                RebuildResolvedAppliedUpgrades();
            }
        }

        private void RebuildResolvedAppliedUpgrades()
        {
            resolvedAppliedUpgrades.Clear();
            if (appliedMultiUpgrades != null)
            {
                for (int i = 0; i < appliedMultiUpgrades.Count; i++)
                {
                    AppliedMultiUpgradeState state = appliedMultiUpgrades[i];
                    if (state != null
                        && state.TryGetActiveUpgrade(out UpgradeSO upgrade)
                        && !resolvedAppliedUpgrades.Contains(upgrade))
                    {
                        resolvedAppliedUpgrades.Add(upgrade);
                    }
                }
            }

            UpgradeSO evolutionUpgrade = selectedEvolution != null ? selectedEvolution.ResolvedUpgrade : null;
            if (evolutionUpgrade != null && !resolvedAppliedUpgrades.Contains(evolutionUpgrade))
            {
                resolvedAppliedUpgrades.Add(evolutionUpgrade);
            }
        }

        private AppliedMultiUpgradeState FindAppliedMultiUpgradeState(MultiUpgradeSO multiUpgrade)
        {
            if (multiUpgrade == null || appliedMultiUpgrades == null)
            {
                return null;
            }

            for (int i = 0; i < appliedMultiUpgrades.Count; i++)
            {
                AppliedMultiUpgradeState state = appliedMultiUpgrades[i];
                if (state != null && state.MultiUpgrade == multiUpgrade)
                {
                    return state;
                }
            }

            return null;
        }
    }

    [SerializeField, Tooltip("Event bus used to mirror runtime progression changes back into roster state.")]
    private UnitEventBus eventBus;

    [SerializeField, Tooltip("Shared XP thresholds by level; index 0 is the threshold from level 1 to 2.")]
    private List<float> xpThresholds = new List<float> { 5f, 10f, 15f, 20f };

    [SerializeField, Tooltip("Inspector-authored roster of player-owned units.")]
    private List<OwnedUnitState> ownedUnits = new List<OwnedUnitState>();
    private bool eventBusSubscribed;

    public IReadOnlyList<float> XpThresholds => xpThresholds;
    public IReadOnlyList<OwnedUnitState> OwnedUnits => ownedUnits;

    private void Awake()
    {
        RegisterWithServiceLocator();
        ResolveEventBus();
        ValidateUnitIds();
        Precompile();
    }

    private void Start()
    {
        ResolveEventBus();
        SubscribeToEventBus();
    }

    private void OnEnable()
    {
        ResolveEventBus();
        SubscribeToEventBus();
    }

    private void OnDisable()
    {
        UnsubscribeFromEventBus();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<UnitStateManager>(this);
    }

    private void OnValidate()
    {
        ValidateUnitIds();
    }

    private void Update()
    {
        ExpireCooldowns();
    }

    /// <summary>
    /// Finds a roster unit by stable unit ID.
    /// </summary>
    public bool TryGetUnit(string unitId, out OwnedUnitState unit)
    {
        for (int i = 0; i < ownedUnits.Count; i++)
        {
            OwnedUnitState candidate = ownedUnits[i];
            if (candidate != null && candidate.UnitId == unitId)
            {
                unit = candidate;
                return true;
            }
        }

        unit = null;
        return false;
    }

    /// <summary>
    /// Returns whether the roster contains a unit with this ID.
    /// </summary>
    public bool HasUnit(string unitId)
    {
        return TryGetUnit(unitId, out _);
    }

    /// <summary>
    /// Returns whether the unit can start a new deployment.
    /// </summary>
    public bool CanDeploy(string unitId)
    {
        if (!TryGetUnit(unitId, out OwnedUnitState unit))
        {
            return false;
        }

        ExpireCooldownIfNeeded(unit, true);
        return unit.UnitPrefab != null
            && unit.CurrentRuntimeInstance == null
            && !unit.IsCoolingDown;
    }

    /// <summary>
    /// Refreshes cached deployment costs for every roster unit without instantiating runtime previews.
    /// </summary>
    public void Precompile()
    {
        for (int i = 0; i < ownedUnits.Count; i++)
        {
            RefreshCompiledDeploymentCost(ownedUnits[i]);
        }
    }

    /// <summary>
    /// Reads the cached deployment cost for a roster unit when precompilation succeeded.
    /// </summary>
    public bool TryGetDeploymentCost(string unitId, out int cost)
    {
        if (TryGetUnit(unitId, out OwnedUnitState unit) && unit.HasCompiledDeploymentCost)
        {
            cost = unit.DeploymentCost;
            return true;
        }

        cost = 0;
        return false;
    }

    /// <summary>
    /// Applies persistent roster upgrades and progression state to a runtime tower instance.
    /// </summary>
    public bool ApplyStateTo(string unitId, TowerEntity tower)
    {
        return tower != null && ApplyStateTo(unitId, tower.gameObject, tower);
    }

    /// <summary>
    /// Applies persistent roster state to a runtime root and tower.
    /// </summary>
    public bool ApplyStateTo(string unitId, GameObject runtimeRoot, TowerEntity tower)
    {
        return ApplyStateTo(unitId, runtimeRoot, tower, true);
    }

    /// <summary>
    /// Applies roster upgrades and progression state into a runtime unit instance without recording it as deployed.
    /// </summary>
    public bool ApplyStateTo(string unitId, GameObject runtimeRoot, TowerEntity tower, bool evaluateThreshold)
    {
        if (!TryGetRuntimeState(unitId, runtimeRoot, tower, out OwnedUnitState unit))
        {
            return false;
        }

        ApplyRuntimeUpgrades(unit, tower);
        InitializeRuntimeProgression(unit, tower, evaluateThreshold);
        return true;
    }

    /// <summary>
    /// Injects all roster state required by a placed runtime unit, then records it as the live deployed instance.
    /// </summary>
    public bool CompleteRuntimeDeployment(string unitId, GameObject runtimeRoot, TowerEntity tower)
    {
        if (!TryGetRuntimeState(unitId, runtimeRoot, tower, out OwnedUnitState unit))
        {
            return false;
        }

        if (!CanDeploy(unitId))
        {
            return false;
        }

        InitializeRuntimeProgression(unit, tower, true);
        unit.SetRuntimeInstance(tower, runtimeRoot);
        ResolveEventBus();
        if (eventBus != null)
        {
            eventBus.RaiseUnitDeployed(new UnitDeployedEvent(unitId, tower, runtimeRoot));
        }

        return true;
    }

    /// <summary>
    /// Records the deployed tower instance for one roster unit.
    /// </summary>
    public bool BindRuntimeInstance(string unitId, TowerEntity tower)
    {
        return tower != null && BindRuntimeInstance(unitId, tower, tower.gameObject);
    }

    /// <summary>
    /// Records the deployed tower and root instance for one roster unit.
    /// </summary>
    public bool BindRuntimeInstance(string unitId, TowerEntity tower, GameObject runtimeRoot)
    {
        if (!TryGetRuntimeState(unitId, runtimeRoot, tower, out OwnedUnitState unit))
        {
            return false;
        }

        ApplyRuntimeUpgrades(unit, tower);
        InitializeRuntimeProgression(unit, tower, true);
        unit.SetRuntimeInstance(tower, runtimeRoot);
        return true;
    }

    /// <summary>
    /// Clears a runtime binding only when the supplied tower matches the recorded instance.
    /// </summary>
    public bool ClearRuntimeInstance(string unitId, TowerEntity tower)
    {
        if (!TryGetUnit(unitId, out OwnedUnitState unit) || unit.CurrentRuntimeInstance != tower)
        {
            return false;
        }

        unit.ClearRuntimeInstance();
        return true;
    }

    /// <summary>
    /// Destroys the deployed runtime unit while preserving persistent XP and upgrades.
    /// </summary>
    public bool RecallUnit(string unitId)
    {
        if (!TryGetUnit(unitId, out OwnedUnitState unit) || unit.CurrentRuntimeInstance == null)
        {
            return false;
        }

        UnitProgression progression = FindRuntimeProgression(unit);
        if (progression != null)
        {
            RecordExperience(unitId, progression.CurrentExperience);
        }

        TowerEntity recalledTower = unit.CurrentRuntimeInstance;
        float cooldownDuration = recalledTower != null
            ? Mathf.Max(0f, recalledTower.GetStat(ENTITY_STATS.DeploymentCooldown))
            : 0f;

        GameObject root = unit.CurrentRuntimeRoot != null
            ? unit.CurrentRuntimeRoot
            : unit.CurrentRuntimeInstance.gameObject;

        unit.ClearRuntimeInstance();
        unit.StartCooldown(cooldownDuration);

        if (root != null)
        {
            Destroy(root);
        }

        ResolveEventBus();
        if (eventBus != null)
        {
            eventBus.RaiseUnitRecalled(new UnitRecalledEvent(unitId));
        }

        return true;
    }

    /// <summary>
    /// Stores the latest runtime XP value on the roster.
    /// </summary>
    public bool RecordExperience(string unitId, float currentExperience)
    {
        if (!TryGetUnit(unitId, out OwnedUnitState unit))
        {
            return false;
        }

        unit.SetExperience(currentExperience);
        return true;
    }

    /// <summary>
    /// Marks a unit as waiting for an upgrade offer selection.
    /// </summary>
    public bool TryBeginUpgradeSelection(string unitId)
    {
        if (!TryGetUnit(unitId, out OwnedUnitState unit) || unit.UpgradePending)
        {
            return false;
        }

        unit.SetUpgradePending(true);
        return true;
    }

    /// <summary>
    /// Returns whether the roster unit is waiting for the player to choose an upgrade.
    /// </summary>
    public bool HasPendingUpgradeSelection(string unitId)
    {
        return TryGetUnit(unitId, out OwnedUnitState unit) && unit.UpgradePending;
    }

    /// <summary>
    /// Records the selected multi-upgrade, advances level, and applies its active level to the deployed tower when present.
    /// </summary>
    public bool RecordSelectedUpgrade(string unitId, MultiUpgradeSO multiUpgrade)
    {
        return RecordSelectedUpgrade(unitId, multiUpgrade, out _, out _);
    }

    /// <summary>
    /// Records the selected multi-upgrade and returns the resolved active level upgrade.
    /// </summary>
    public bool RecordSelectedUpgrade(
        string unitId,
        MultiUpgradeSO multiUpgrade,
        out UpgradeSO selectedUpgrade,
        out int selectedUpgradeLevel)
    {
        return RecordSelectedUpgrade(unitId, multiUpgrade, null, out selectedUpgrade, out selectedUpgradeLevel);
    }

    /// <summary>
    /// Records the selected upgrade choice and returns the resolved tower-facing upgrade leaf.
    /// </summary>
    public bool RecordSelectedUpgrade(
        string unitId,
        MultiUpgradeSO multiUpgrade,
        EvolutionSO evolution,
        out UpgradeSO selectedUpgrade,
        out int selectedUpgradeLevel)
    {
        selectedUpgrade = null;
        selectedUpgradeLevel = 0;

        if (!TryGetUnit(unitId, out OwnedUnitState unit)
            || !unit.UpgradePending
            || (multiUpgrade != null && evolution != null))
        {
            return false;
        }

        if (multiUpgrade != null
            && !unit.TryGetNextMultiUpgradeLevel(multiUpgrade, out _, out selectedUpgradeLevel, out selectedUpgrade))
        {
            return false;
        }

        if (evolution != null)
        {
            if (!unit.CanSelectEvolution(evolution))
            {
                return false;
            }

            selectedUpgrade = evolution.ResolvedUpgrade;
            selectedUpgradeLevel = 1;
        }

        unit.SetUpgradePending(false);
        unit.AdvanceLevel();

        if (multiUpgrade != null
            && unit.ApplyNextMultiUpgradeLevel(
                multiUpgrade,
                out UpgradeSO previousUpgrade,
                out selectedUpgrade,
                out selectedUpgradeLevel)
            && unit.CurrentRuntimeInstance != null)
        {
            unit.CurrentRuntimeInstance.ReplaceUpgrade(previousUpgrade, selectedUpgrade);
        }

        if (evolution != null
            && unit.TrySelectEvolution(evolution, out selectedUpgrade)
            && unit.CurrentRuntimeInstance != null)
        {
            unit.CurrentRuntimeInstance.AddUpgrade(selectedUpgrade);
        }

        RefreshCompiledDeploymentCost(unit);
        RefreshRuntimeProgression(unit, true);
        return true;
    }

    /// <summary>
    /// Debug endpoint that force-applies an evolution and its prerequisites to a deployed roster unit.
    /// </summary>
    public bool DebugForceEvolution(string unitId, EvolutionSO evolution)
    {
        if (!TryGetUnit(unitId, out OwnedUnitState unit)
            || unit.CurrentRuntimeInstance == null
            || evolution == null
            || !evolution.HasResolvedUpgrade)
        {
            return false;
        }

        List<UpgradeSO> previousUpgrades = unit.DebugCreateAppliedUpgradeSnapshot();

        IReadOnlyList<EvolutionSO.Prerequisite> prerequisites = evolution.Prerequisites;
        for (int i = 0; i < prerequisites.Count; i++)
        {
            EvolutionSO.Prerequisite prerequisite = prerequisites[i];
            unit.DebugForceMultiUpgradeLevel(prerequisite.MultiUpgrade, prerequisite.MinimumLevel);
        }

        if (!unit.DebugForceEvolution(evolution))
        {
            return false;
        }

        List<UpgradeSO> currentUpgrades = unit.DebugCreateAppliedUpgradeSnapshot();
        ReconcileRuntimeUpgrades(unit.CurrentRuntimeInstance, previousUpgrades, currentUpgrades);
        RefreshCompiledDeploymentCost(unit);
        RefreshRuntimeProgression(unit, false);
        RaiseDebugUpgradeSelected(unit, null, evolution, evolution.ResolvedUpgrade, 1);
        return true;
    }

    /// <summary>
    /// Debug endpoint that adds enough XP to a deployed roster unit to trigger its normal upgrade offer flow.
    /// </summary>
    public bool DebugTriggerUpgradeThreshold(string unitId)
    {
        if (!TryGetUnit(unitId, out OwnedUnitState unit)
            || unit.CurrentRuntimeInstance == null
            || unit.UpgradePending
            || !TryGetNextExperienceThreshold(unit, out float threshold))
        {
            return false;
        }

        UnitProgression progression = FindRuntimeProgression(unit);
        if (progression == null)
        {
            return false;
        }

        float experienceNeeded = threshold - progression.CurrentExperience;
        progression.AddExperience(Mathf.Max(0.001f, experienceNeeded));
        return true;
    }

    /// <summary>
    /// Debug endpoint that clears all upgrade and evolution choices from a deployed roster unit.
    /// </summary>
    public bool DebugResetUpgrades(string unitId)
    {
        if (!TryGetUnit(unitId, out OwnedUnitState unit)
            || unit.CurrentRuntimeInstance == null)
        {
            return false;
        }

        List<UpgradeSO> previousUpgrades = unit.DebugCreateAppliedUpgradeSnapshot();
        bool hadUpgrades = unit.DebugClearUpgrades();
        List<UpgradeSO> currentUpgrades = unit.DebugCreateAppliedUpgradeSnapshot();
        ReconcileRuntimeUpgrades(unit.CurrentRuntimeInstance, previousUpgrades, currentUpgrades);
        RefreshCompiledDeploymentCost(unit);
        RefreshRuntimeProgression(unit, false);
        RaiseDebugUpgradeSelected(unit, null, null, null, 0);
        return hadUpgrades;
    }

    /// <summary>
    /// Reads the next XP threshold for a roster unit.
    /// </summary>
    public bool TryGetNextExperienceThreshold(string unitId, out float threshold)
    {
        if (TryGetUnit(unitId, out OwnedUnitState unit))
        {
            return TryGetNextExperienceThreshold(unit, out threshold);
        }

        threshold = 0f;
        return false;
    }

    private bool TryGetNextExperienceThreshold(OwnedUnitState unit, out float threshold)
    {
        int thresholdIndex = unit != null ? unit.Level - 1 : -1;
        if (xpThresholds != null
            && thresholdIndex >= 0
            && thresholdIndex < xpThresholds.Count)
        {
            threshold = Mathf.Max(0f, xpThresholds[thresholdIndex]);
            return true;
        }

        threshold = 0f;
        return false;
    }

    private void HandleUnitExperienceChanged(UnitExperienceChangedEvent eventData)
    {
        RecordExperience(eventData.UnitId, eventData.CurrentExperience);
    }

    private void RefreshCompiledDeploymentCost(OwnedUnitState unit)
    {
        if (unit == null || unit.UnitPrefab == null)
        {
            if (unit != null)
            {
                unit.ClearCompiledDeploymentCost();
                RaiseUnitDeploymentCostCompiled(unit);
            }

            return;
        }

        float cost = unit.UnitPrefab.CalculateFinalStat(ENTITY_STATS.DeploymentCost, unit.AppliedUpgrades);
        unit.SetCompiledDeploymentCost(Mathf.CeilToInt(Mathf.Max(0f, cost)));
        RaiseUnitDeploymentCostCompiled(unit);
    }

    private void RaiseUnitDeploymentCostCompiled(OwnedUnitState unit)
    {
        if (unit == null || string.IsNullOrWhiteSpace(unit.UnitId))
        {
            return;
        }

        ResolveEventBus();
        if (eventBus != null)
        {
            eventBus.RaiseUnitDeploymentCostCompiled(new UnitDeploymentCostCompiledEvent(
                unit.UnitId,
                unit.HasCompiledDeploymentCost,
                unit.DeploymentCost));
        }
    }

    private void RaiseDebugUpgradeSelected(
        OwnedUnitState unit,
        MultiUpgradeSO selectedMultiUpgrade,
        EvolutionSO selectedEvolution,
        UpgradeSO selectedUpgrade,
        int selectedUpgradeLevel)
    {
        if (unit == null || string.IsNullOrWhiteSpace(unit.UnitId))
        {
            return;
        }

        ResolveEventBus();
        if (eventBus == null)
        {
            return;
        }

        bool hasNextExperienceThreshold = TryGetNextExperienceThreshold(
            unit.UnitId,
            out float nextExperienceThreshold);

        eventBus.RaiseUnitUpgradeSelected(new UnitUpgradeSelectedEvent(
            unit.UnitId,
            selectedMultiUpgrade,
            selectedUpgrade,
            selectedUpgradeLevel,
            unit.Level,
            unit.Experience,
            hasNextExperienceThreshold,
            nextExperienceThreshold,
            selectedEvolution));
    }

    private static void ReconcileRuntimeUpgrades(
        TowerEntity tower,
        IReadOnlyList<UpgradeSO> previousUpgrades,
        IReadOnlyList<UpgradeSO> currentUpgrades)
    {
        if (tower == null)
        {
            return;
        }

        for (int i = 0; i < previousUpgrades.Count; i++)
        {
            UpgradeSO previousUpgrade = previousUpgrades[i];
            if (previousUpgrade != null && !ContainsUpgrade(currentUpgrades, previousUpgrade))
            {
                tower.RemoveUpgrade(previousUpgrade);
            }
        }

        for (int i = 0; i < currentUpgrades.Count; i++)
        {
            UpgradeSO currentUpgrade = currentUpgrades[i];
            if (currentUpgrade != null && !ContainsUpgrade(previousUpgrades, currentUpgrade))
            {
                tower.AddUpgrade(currentUpgrade);
            }
        }
    }

    private static bool ContainsUpgrade(IReadOnlyList<UpgradeSO> upgrades, UpgradeSO upgrade)
    {
        if (upgrade == null || upgrades == null)
        {
            return false;
        }

        for (int i = 0; i < upgrades.Count; i++)
        {
            if (upgrades[i] == upgrade)
            {
                return true;
            }
        }

        return false;
    }

    private void ExpireCooldowns()
    {
        for (int i = 0; i < ownedUnits.Count; i++)
        {
            ExpireCooldownIfNeeded(ownedUnits[i], true);
        }
    }

    private bool ExpireCooldownIfNeeded(OwnedUnitState unit, bool raiseEvent)
    {
        if (unit == null || !unit.TryEndExpiredCooldown())
        {
            return false;
        }

        if (raiseEvent)
        {
            RaiseUnitCooldownEnded(unit.UnitId);
        }

        return true;
    }

    private void RaiseUnitCooldownEnded(string unitId)
    {
        ResolveEventBus();
        if (eventBus != null)
        {
            eventBus.RaiseUnitCooldownEnded(new UnitCooldownEndedEvent(unitId));
        }
    }

    private void InitializeProgression(OwnedUnitState unit, UnitProgression progression, bool evaluateThreshold)
    {
        ResolveEventBus();

        bool hasThreshold = TryGetNextExperienceThreshold(unit, out float threshold);
        progression.Initialize(
            unit.UnitId,
            unit.Level,
            unit.Experience,
            threshold,
            hasThreshold,
            unit.UpgradePending,
            eventBus,
            evaluateThreshold);
    }

    private bool TryGetRuntimeState(
        string unitId,
        GameObject runtimeRoot,
        TowerEntity tower,
        out OwnedUnitState unit)
    {
        if (runtimeRoot == null || tower == null || !TryGetUnit(unitId, out unit))
        {
            unit = null;
            return false;
        }

        return true;
    }

    private void ApplyRuntimeUpgrades(OwnedUnitState unit, TowerEntity tower)
    {
        // This is the roster-to-runtime bridge; TowerEntity remains responsible for compiling upgrade effects.
        IReadOnlyList<UpgradeSO> appliedUpgrades = unit.AppliedUpgrades;
        for (int i = 0; i < appliedUpgrades.Count; i++)
        {
            tower.AddUpgrade(appliedUpgrades[i]);
        }
    }

    private void InitializeRuntimeProgression(
        OwnedUnitState unit,
        TowerEntity tower,
        bool evaluateThreshold)
    {
        InitializeProgression(unit, EnsureRuntimeProgression(tower), evaluateThreshold);
    }

    private UnitProgression EnsureRuntimeProgression(TowerEntity tower)
    {
        UnitProgression progression = tower.GetComponent<UnitProgression>();
        if (progression == null)
        {
            progression = tower.gameObject.AddComponent<UnitProgression>();
        }

        return progression;
    }

    private void RefreshRuntimeProgression(OwnedUnitState unit, bool evaluateThreshold)
    {
        UnitProgression progression = FindRuntimeProgression(unit);
        if (progression != null)
        {
            InitializeProgression(unit, progression, evaluateThreshold);
        }
    }

    private UnitProgression FindRuntimeProgression(OwnedUnitState unit)
    {
        if (unit.CurrentRuntimeRoot != null)
        {
            return unit.CurrentRuntimeRoot.GetComponentInChildren<UnitProgression>(true);
        }

        return unit.CurrentRuntimeInstance != null
            ? unit.CurrentRuntimeInstance.GetComponentInChildren<UnitProgression>(true)
            : null;
    }

    private void ResolveEventBus()
    {
        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }
    }

    private void SubscribeToEventBus()
    {
        if (eventBusSubscribed)
        {
            return;
        }

        if (eventBus == null)
        {
            ResolveEventBus();
        }

        if (eventBus == null)
        {
            return;
        }

        eventBus.UnitExperienceChanged += HandleUnitExperienceChanged;
        eventBusSubscribed = true;
    }

    private void UnsubscribeFromEventBus()
    {
        if (!eventBusSubscribed || eventBus == null)
        {
            return;
        }

        eventBus.UnitExperienceChanged -= HandleUnitExperienceChanged;
        eventBusSubscribed = false;
    }

    private void RegisterWithServiceLocator()
    {
        if (ServiceLocator.TryResolve<UnitStateManager>(out UnitStateManager existingStateManager)
            && existingStateManager != null
            && existingStateManager != this)
        {
            Debug.LogWarning(
                $"{nameof(UnitStateManager)} on '{name}' replaced the previously registered {nameof(UnitStateManager)} on '{existingStateManager.name}'.",
                this);
        }

        ServiceLocator.Register<UnitStateManager>(this);
    }

    private void ValidateUnitIds()
    {
        HashSet<string> seenIds = new HashSet<string>();
        for (int i = 0; i < ownedUnits.Count; i++)
        {
            OwnedUnitState unit = ownedUnits[i];
            if (unit == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(unit.UnitId))
            {
                Debug.LogWarning($"{nameof(UnitStateManager)} has an owned unit with an empty unitId.", this);
                continue;
            }

            if (!seenIds.Add(unit.UnitId))
            {
                Debug.LogError($"{nameof(UnitStateManager)} has duplicate unitId '{unit.UnitId}'.", this);
            }
        }
    }
}
