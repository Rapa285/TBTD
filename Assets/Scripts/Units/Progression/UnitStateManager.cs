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

        [SerializeField, Tooltip("XP thresholds by level; index 0 is the threshold from level 1 to 2.")]
        private List<float> xpThresholds = new List<float>();

        [SerializeField, Tooltip("Current persistent level for this owned unit.")]
        private int level = 1;

        [SerializeField, Tooltip("Current persistent XP stored for this owned unit.")]
        private float experience;

        [SerializeField, Tooltip("Whether this unit is waiting for an upgrade selection.")]
        private bool upgradePending;

        [SerializeField, Tooltip("Append-only list of upgrades selected for this owned unit. Runtime stat and weapon composition still happens inside TowerEntity.")]
        private List<UpgradeSO> appliedUpgrades = new List<UpgradeSO>();

        [NonSerialized] private TowerEntity currentRuntimeInstance;
        [NonSerialized] private GameObject currentRuntimeRoot;
        [NonSerialized] private float cooldownDuration;
        [NonSerialized] private float cooldownEndTime;
        [NonSerialized] private bool cooldownActive;

        public string UnitId => unitId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public TowerEntity UnitPrefab => unitPrefab;
        public IReadOnlyList<float> XpThresholds => xpThresholds;
        public int Level => Mathf.Max(1, level);
        public float Experience => Mathf.Max(0f, experience);
        public bool UpgradePending => upgradePending;
        public IReadOnlyList<UpgradeSO> AppliedUpgrades => appliedUpgrades;
        public TowerEntity CurrentRuntimeInstance => currentRuntimeInstance;
        public GameObject CurrentRuntimeRoot => currentRuntimeRoot;
        public bool IsDeployed => currentRuntimeInstance != null;
        public bool IsCoolingDown => cooldownActive && CooldownRemaining > 0f;
        public float CooldownDuration => cooldownActive ? cooldownDuration : 0f;
        public float CooldownRemaining => cooldownActive ? Mathf.Max(0f, cooldownEndTime - Time.time) : 0f;
        public float CooldownNormalizedRemaining => cooldownDuration > 0f
            ? Mathf.Clamp01(CooldownRemaining / cooldownDuration)
            : 0f;
        public bool HasNextExperienceThreshold => TryGetNextExperienceThreshold(out _);
        public float NextExperienceThreshold => TryGetNextExperienceThreshold(out float threshold) ? threshold : 0f;

        /// <summary>
        /// Returns whether this unit has already selected the given upgrade asset.
        /// </summary>
        public bool HasAppliedUpgrade(UpgradeSO upgrade)
        {
            return upgrade != null && appliedUpgrades.Contains(upgrade);
        }

        /// <summary>
        /// Reads the next XP threshold for the current level when one exists.
        /// </summary>
        public bool TryGetNextExperienceThreshold(out float threshold)
        {
            int thresholdIndex = Level - 1;
            if (thresholdIndex >= 0 && thresholdIndex < xpThresholds.Count)
            {
                threshold = Mathf.Max(0f, xpThresholds[thresholdIndex]);
                return true;
            }

            threshold = 0f;
            return false;
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

        internal bool AddAppliedUpgrade(UpgradeSO upgrade)
        {
            if (upgrade == null || appliedUpgrades.Contains(upgrade))
            {
                return false;
            }

            appliedUpgrades.Add(upgrade);
            return true;
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
    }

    [SerializeField, Tooltip("Event bus used to mirror runtime progression changes back into roster state.")]
    private UnitEventBus eventBus;

    [SerializeField, Tooltip("Inspector-authored roster of player-owned units.")]
    private List<OwnedUnitState> ownedUnits = new List<OwnedUnitState>();
    private bool eventBusSubscribed;

    public IReadOnlyList<OwnedUnitState> OwnedUnits => ownedUnits;

    private void Awake()
    {
        RegisterWithServiceLocator();
        ResolveEventBus();
        ValidateUnitIds();
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
    /// Records the selected upgrade, advances level, and applies it immediately to the deployed tower when present.
    /// </summary>
    public bool RecordSelectedUpgrade(string unitId, UpgradeSO upgrade)
    {
        if (!TryGetUnit(unitId, out OwnedUnitState unit) || !unit.UpgradePending)
        {
            return false;
        }

        unit.SetUpgradePending(false);
        unit.AdvanceLevel();

        if (upgrade != null && unit.AddAppliedUpgrade(upgrade) && unit.CurrentRuntimeInstance != null)
        {
            unit.CurrentRuntimeInstance.AddUpgrade(upgrade);
        }

        RefreshRuntimeProgression(unit, true);
        return true;
    }

    /// <summary>
    /// Reads the next XP threshold for a roster unit.
    /// </summary>
    public bool TryGetNextExperienceThreshold(string unitId, out float threshold)
    {
        if (TryGetUnit(unitId, out OwnedUnitState unit))
        {
            return unit.TryGetNextExperienceThreshold(out threshold);
        }

        threshold = 0f;
        return false;
    }

    private void HandleUnitExperienceChanged(UnitExperienceChangedEvent eventData)
    {
        RecordExperience(eventData.UnitId, eventData.CurrentExperience);
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

        bool hasThreshold = unit.TryGetNextExperienceThreshold(out float threshold);
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
        for (int i = 0; i < unit.AppliedUpgrades.Count; i++)
        {
            tower.AddUpgrade(unit.AppliedUpgrades[i]);
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
