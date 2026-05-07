using System;
using UnityEngine;

/// <summary>
/// Raised after a roster-managed runtime unit has been bound as deployed.
/// </summary>
public struct UnitDeployedEvent
{
    public string UnitId { get; }
    public TowerEntity Tower { get; }
    public GameObject RuntimeRoot { get; }

    public UnitDeployedEvent(string unitId, TowerEntity tower, GameObject runtimeRoot)
    {
        UnitId = unitId;
        Tower = tower;
        RuntimeRoot = runtimeRoot;
    }
}

/// <summary>
/// Raised when a deployed unit gains experience.
/// </summary>
public struct UnitExperienceChangedEvent
{
    public string UnitId { get; }
    public float PreviousExperience { get; }
    public float CurrentExperience { get; }
    public float Delta { get; }
    public int Level { get; }

    public UnitExperienceChangedEvent(string unitId, float previousExperience, float currentExperience, float delta, int level)
    {
        UnitId = unitId;
        PreviousExperience = previousExperience;
        CurrentExperience = currentExperience;
        Delta = delta;
        Level = level;
    }
}

/// <summary>
/// Raised when a deployed unit reaches its next upgrade threshold.
/// </summary>
public struct UnitUpgradeThresholdReachedEvent
{
    public string UnitId { get; }
    public float CurrentExperience { get; }
    public float Threshold { get; }
    public int Level { get; }

    public UnitUpgradeThresholdReachedEvent(string unitId, float currentExperience, float threshold, int level)
    {
        UnitId = unitId;
        CurrentExperience = currentExperience;
        Threshold = threshold;
        Level = level;
    }
}

/// <summary>
/// One generated upgrade offer choice resolved from a multi-upgrade line to its next active level.
/// </summary>
public readonly struct UnitUpgradeOfferChoice
{
    public MultiUpgradeSO MultiUpgrade { get; }
    public UpgradeSO ResolvedUpgrade { get; }
    public int CurrentLevel { get; }
    public int NextLevel { get; }
    public int MaxLevel { get; }
    public bool IsValid => MultiUpgrade != null
        && ResolvedUpgrade != null
        && NextLevel > CurrentLevel
        && NextLevel <= MaxLevel;

    public UnitUpgradeOfferChoice(
        MultiUpgradeSO multiUpgrade,
        UpgradeSO resolvedUpgrade,
        int currentLevel,
        int nextLevel,
        int maxLevel)
    {
        MultiUpgrade = multiUpgrade;
        ResolvedUpgrade = resolvedUpgrade;
        CurrentLevel = Mathf.Max(0, currentLevel);
        NextLevel = Mathf.Max(0, nextLevel);
        MaxLevel = Mathf.Max(0, maxLevel);
    }
}

/// <summary>
/// Raised when upgrade choices are available for a unit.
/// </summary>
public struct UnitUpgradeChoicesOfferedEvent
{
    public string UnitId { get; }
    public UnitUpgradeOfferChoice[] Choices { get; }

    public UnitUpgradeChoicesOfferedEvent(string unitId, UnitUpgradeOfferChoice[] choices)
    {
        UnitId = unitId;
        Choices = choices;
    }
}

/// <summary>
/// Raised by UI when a player requests one pending upgrade choice.
/// </summary>
public struct UnitUpgradeChoiceRequestedEvent
{
    public string UnitId { get; }
    public int ChoiceIndex { get; }

    public UnitUpgradeChoiceRequestedEvent(string unitId, int choiceIndex)
    {
        UnitId = unitId;
        ChoiceIndex = choiceIndex;
    }
}

/// <summary>
/// Raised after a pending upgrade choice has been recorded on the roster.
/// </summary>
public struct UnitUpgradeSelectedEvent
{
    public string UnitId { get; }
    public MultiUpgradeSO SelectedMultiUpgrade { get; }
    public UpgradeSO SelectedUpgrade { get; }
    public int SelectedUpgradeLevel { get; }
    public int NewLevel { get; }
    public float CurrentExperience { get; }
    public bool HasNextExperienceThreshold { get; }
    public float NextExperienceThreshold { get; }

    public UnitUpgradeSelectedEvent(
        string unitId,
        MultiUpgradeSO selectedMultiUpgrade,
        UpgradeSO selectedUpgrade,
        int selectedUpgradeLevel,
        int newLevel,
        float currentExperience,
        bool hasNextExperienceThreshold,
        float nextExperienceThreshold)
    {
        UnitId = unitId;
        SelectedMultiUpgrade = selectedMultiUpgrade;
        SelectedUpgrade = selectedUpgrade;
        SelectedUpgradeLevel = Mathf.Max(0, selectedUpgradeLevel);
        NewLevel = newLevel;
        CurrentExperience = currentExperience;
        HasNextExperienceThreshold = hasNextExperienceThreshold;
        NextExperienceThreshold = nextExperienceThreshold;
    }
}

/// <summary>
/// Raised after a deployed unit has been recalled and its runtime instance removed.
/// </summary>
public struct UnitRecalledEvent
{
    public string UnitId { get; }

    public UnitRecalledEvent(string unitId)
    {
        UnitId = unitId;
    }
}

/// <summary>
/// Raised after a recalled unit's deployment cooldown has elapsed.
/// </summary>
public struct UnitCooldownEndedEvent
{
    public string UnitId { get; }

    public UnitCooldownEndedEvent(string unitId)
    {
        UnitId = unitId;
    }
}

/// <summary>
/// Raised after a tower spends one or more ammo units on its primary weapon.
/// </summary>
public struct UnitAmmoConsumedEvent
{
    public string UnitId { get; }
    public TowerEntity Tower { get; }
    public AttackBehaviour AttackBehaviour { get; }
    public int AmountConsumed { get; }
    public int CurrentAmmoUnits { get; }
    public int MaxAmmoUnits { get; }

    public UnitAmmoConsumedEvent(
        string unitId,
        TowerEntity tower,
        AttackBehaviour attackBehaviour,
        int amountConsumed,
        int currentAmmoUnits,
        int maxAmmoUnits)
    {
        UnitId = unitId;
        Tower = tower;
        AttackBehaviour = attackBehaviour;
        AmountConsumed = Mathf.Max(0, amountConsumed);
        CurrentAmmoUnits = Mathf.Max(0, currentAmmoUnits);
        MaxAmmoUnits = Mathf.Max(0, maxAmmoUnits);
    }
}

/// <summary>
/// Raised after a deployed tower refreshes its runtime state outside the initial deployment activation.
/// </summary>
public struct TowerModifiedEvent
{
    public string UnitId { get; }
    public TowerEntity Tower { get; }

    public TowerModifiedEvent(string unitId, TowerEntity tower)
    {
        UnitId = unitId;
        Tower = tower;
    }
}

/// <summary>
/// Raised after the player's currency balance changes.
/// </summary>
public struct CurrencyChangedEvent
{
    public int PreviousCurrency { get; }
    public int CurrentCurrency { get; }
    public int Delta { get; }

    public CurrencyChangedEvent(int previousCurrency, int currentCurrency, int delta)
    {
        PreviousCurrency = Mathf.Max(0, previousCurrency);
        CurrentCurrency = Mathf.Max(0, currentCurrency);
        Delta = delta;
    }
}

/// <summary>
/// Raised after a roster unit's cached deployment cost is compiled or cleared.
/// </summary>
public struct UnitDeploymentCostCompiledEvent
{
    public string UnitId { get; }
    public bool HasCost { get; }
    public int Cost { get; }

    public UnitDeploymentCostCompiledEvent(string unitId, bool hasCost, int cost)
    {
        UnitId = unitId;
        HasCost = hasCost;
        Cost = Mathf.Max(0, cost);
    }
}

/// <summary>
/// Raised after a deployment preview has been created and prepared.
/// </summary>
public struct UnitDeploymentPreviewStartedEvent
{
    public string UnitId { get; }
    public GameObject UnitPrefab { get; }
    public TowerEntity Tower { get; }
    public GameObject RuntimeRoot { get; }
    public bool WasCompleted { get; }

    public UnitDeploymentPreviewStartedEvent(
        string unitId,
        GameObject unitPrefab,
        TowerEntity tower,
        GameObject runtimeRoot)
    {
        UnitId = unitId;
        UnitPrefab = unitPrefab;
        Tower = tower;
        RuntimeRoot = runtimeRoot;
        WasCompleted = false;
    }
}

/// <summary>
/// Raised after a deployment preview has ended through cancellation or completion.
/// </summary>
public struct UnitDeploymentPreviewEndedEvent
{
    public string UnitId { get; }
    public GameObject UnitPrefab { get; }
    public TowerEntity Tower { get; }
    public GameObject RuntimeRoot { get; }
    public bool WasCompleted { get; }

    public UnitDeploymentPreviewEndedEvent(
        string unitId,
        GameObject unitPrefab,
        TowerEntity tower,
        GameObject runtimeRoot,
        bool wasCompleted)
    {
        UnitId = unitId;
        UnitPrefab = unitPrefab;
        Tower = tower;
        RuntimeRoot = runtimeRoot;
        WasCompleted = wasCompleted;
    }
}

/// <summary>
/// Lightweight scene event hub for unit progression, upgrade, and recall notifications.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class UnitEventBus : MonoBehaviour
{
    public event Action<UnitDeployedEvent> UnitDeployed;
    public event Action<UnitExperienceChangedEvent> UnitExperienceChanged;
    public event Action<UnitUpgradeThresholdReachedEvent> UnitUpgradeThresholdReached;
    public event Action<UnitUpgradeChoicesOfferedEvent> UnitUpgradeChoicesOffered;
    public event Action<UnitUpgradeChoiceRequestedEvent> UnitUpgradeChoiceRequested;
    public event Action<UnitUpgradeSelectedEvent> UnitUpgradeSelected;
    public event Action<UnitRecalledEvent> UnitRecalled;
    public event Action<UnitCooldownEndedEvent> UnitCooldownEnded;
    public event Action<UnitAmmoConsumedEvent> UnitAmmoConsumed;
    public event Action<TowerModifiedEvent> TowerModified;
    public event Action<CurrencyChangedEvent> CurrencyChanged;
    public event Action<UnitDeploymentCostCompiledEvent> UnitDeploymentCostCompiled;
    public event Action<UnitDeploymentPreviewStartedEvent> UnitDeploymentPreviewStarted;
    public event Action<UnitDeploymentPreviewEndedEvent> UnitDeploymentPreviewEnded;

    private void Awake()
    {
        RegisterWithServiceLocator();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<UnitEventBus>(this);
    }

    /// <summary>
    /// Publishes that a roster-managed unit has completed deployment binding.
    /// </summary>
    public void RaiseUnitDeployed(UnitDeployedEvent eventData)
    {
        UnitDeployed?.Invoke(eventData);
    }

    /// <summary>
    /// Publishes a unit experience change.
    /// </summary>
    public void RaiseUnitExperienceChanged(UnitExperienceChangedEvent eventData)
    {
        UnitExperienceChanged?.Invoke(eventData);
    }

    /// <summary>
    /// Publishes that a unit has reached an upgrade threshold.
    /// </summary>
    public void RaiseUnitUpgradeThresholdReached(UnitUpgradeThresholdReachedEvent eventData)
    {
        UnitUpgradeThresholdReached?.Invoke(eventData);
    }

    /// <summary>
    /// Publishes the generated upgrade choices for a unit.
    /// </summary>
    public void RaiseUnitUpgradeChoicesOffered(UnitUpgradeChoicesOfferedEvent eventData)
    {
        UnitUpgradeChoicesOffered?.Invoke(eventData);
    }

    /// <summary>
    /// Publishes that UI requested one pending upgrade choice.
    /// </summary>
    public void RaiseUnitUpgradeChoiceRequested(UnitUpgradeChoiceRequestedEvent eventData)
    {
        UnitUpgradeChoiceRequested?.Invoke(eventData);
    }

    /// <summary>
    /// Publishes the selected multi-upgrade line, resolved upgrade level, and resulting progression state.
    /// </summary>
    public void RaiseUnitUpgradeSelected(UnitUpgradeSelectedEvent eventData)
    {
        UnitUpgradeSelected?.Invoke(eventData);
    }

    /// <summary>
    /// Publishes that a unit was recalled.
    /// </summary>
    public void RaiseUnitRecalled(UnitRecalledEvent eventData)
    {
        UnitRecalled?.Invoke(eventData);
    }

    /// <summary>
    /// Publishes that a recalled unit can deploy again after cooldown.
    /// </summary>
    public void RaiseUnitCooldownEnded(UnitCooldownEndedEvent eventData)
    {
        UnitCooldownEnded?.Invoke(eventData);
    }

    /// <summary>
    /// Publishes that a tower consumed ammo through its primary weapon.
    /// </summary>
    public void RaiseUnitAmmoConsumed(UnitAmmoConsumedEvent eventData)
    {
        UnitAmmoConsumed?.Invoke(eventData);
    }

    /// <summary>
    /// Publishes that a deployed tower refreshed its runtime state after deployment.
    /// </summary>
    public void RaiseTowerModified(TowerModifiedEvent eventData)
    {
        TowerModified?.Invoke(eventData);
    }

    /// <summary>
    /// Publishes that the player's currency balance changed.
    /// </summary>
    public void RaiseCurrencyChanged(CurrencyChangedEvent eventData)
    {
        CurrencyChanged?.Invoke(eventData);
    }

    /// <summary>
    /// Publishes that a roster unit's cached deployment cost changed.
    /// </summary>
    public void RaiseUnitDeploymentCostCompiled(UnitDeploymentCostCompiledEvent eventData)
    {
        UnitDeploymentCostCompiled?.Invoke(eventData);
    }

    /// <summary>
    /// Publishes that a deployment preview has started.
    /// </summary>
    public void RaiseUnitDeploymentPreviewStarted(UnitDeploymentPreviewStartedEvent eventData)
    {
        UnitDeploymentPreviewStarted?.Invoke(eventData);
    }

    /// <summary>
    /// Publishes that a deployment preview has ended.
    /// </summary>
    public void RaiseUnitDeploymentPreviewEnded(UnitDeploymentPreviewEndedEvent eventData)
    {
        UnitDeploymentPreviewEnded?.Invoke(eventData);
    }

    private void RegisterWithServiceLocator()
    {
        if (ServiceLocator.TryResolve<UnitEventBus>(out UnitEventBus existingEventBus)
            && existingEventBus != null
            && existingEventBus != this)
        {
            Debug.LogWarning(
                $"{nameof(UnitEventBus)} on '{name}' replaced the previously registered {nameof(UnitEventBus)} on '{existingEventBus.name}'.",
                this);
        }

        ServiceLocator.Register<UnitEventBus>(this);
    }
}
