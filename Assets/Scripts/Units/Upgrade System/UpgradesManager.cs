using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates shared-pool upgrade offers and records selected upgrades for roster units.
/// </summary>
public class UpgradesManager : MonoBehaviour
{
    [SerializeField, Tooltip("Event bus used to listen for level-up requests and publish offers/selections.")]
    private UnitEventBus eventBus;

    [SerializeField, Tooltip("Roster manager that owns unit progression state and applied upgrade lists.")]
    private UnitStateManager unitStateManager;

    [SerializeField, Tooltip("Shared pool of upgrade assets that every current unit can be offered.")]
    private List<UpgradeSO> upgradePool = new List<UpgradeSO>();

    [SerializeField, Min(0), Tooltip("Maximum number of unique choices generated for one offer.")]
    private int upgradeChoiceCount = 3;

    private readonly Dictionary<string, List<UpgradeSO>> pendingOffers = new Dictionary<string, List<UpgradeSO>>();
    private bool eventBusSubscribed;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();
        SubscribeToEventBus();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToEventBus();
    }

    private void OnDisable()
    {
        UnsubscribeFromEventBus();
    }

    /// <summary>
    /// Returns whether the roster unit currently has an unresolved generated offer.
    /// </summary>
    public bool HasPendingOffer(string unitId)
    {
        return pendingOffers.ContainsKey(unitId);
    }

    /// <summary>
    /// Gets the pending offer choices for a unit without exposing the mutable backing list.
    /// </summary>
    public bool TryGetPendingChoices(string unitId, out IReadOnlyList<UpgradeSO> choices)
    {
        if (pendingOffers.TryGetValue(unitId, out List<UpgradeSO> offer))
        {
            choices = offer;
            return true;
        }

        choices = null;
        return false;
    }

    /// <summary>
    /// Selects one pending upgrade choice by UI index from the currently stored offer.
    /// </summary>
    public bool SelectUpgrade(string unitId, int choiceIndex)
    {
        if (!pendingOffers.TryGetValue(unitId, out List<UpgradeSO> offer)
            || choiceIndex < 0
            || choiceIndex >= offer.Count)
        {
            return false;
        }

        return SelectPendingUpgrade(unitId, offer[choiceIndex]);
    }

    /// <summary>
    /// Selects one pending upgrade choice by asset reference from the currently stored offer.
    /// </summary>
    public bool SelectUpgrade(string unitId, UpgradeSO upgrade)
    {
        if (upgrade == null
            || !pendingOffers.TryGetValue(unitId, out List<UpgradeSO> offer)
            || !offer.Contains(upgrade))
        {
            return false;
        }

        return SelectPendingUpgrade(unitId, upgrade);
    }

    private void HandleUnitUpgradeThresholdReached(UnitUpgradeThresholdReachedEvent eventData)
    {
        string unitId = eventData.UnitId;
        if (string.IsNullOrWhiteSpace(unitId)
            || unitStateManager == null
            || pendingOffers.ContainsKey(unitId)
            || !unitStateManager.TryGetUnit(unitId, out UnitStateManager.OwnedUnitState unit)
            || !unitStateManager.TryBeginUpgradeSelection(unitId))
        {
            return;
        }

        List<UpgradeSO> offer = BuildOffer(unit);
        if (offer.Count == 0)
        {
            RecordSelection(unitId, null);
            return;
        }

        pendingOffers[unitId] = offer;

        ResolveReferences();
        if (eventBus != null)
        {
            eventBus.RaiseUnitUpgradeChoicesOffered(new UnitUpgradeChoicesOfferedEvent(unitId, offer.ToArray()));
        }
    }

    private void HandleUnitUpgradeChoiceRequested(UnitUpgradeChoiceRequestedEvent eventData)
    {
        if (string.IsNullOrWhiteSpace(eventData.UnitId))
        {
            return;
        }

        SelectUpgrade(eventData.UnitId, eventData.ChoiceIndex);
    }

    private List<UpgradeSO> BuildOffer(UnitStateManager.OwnedUnitState unit)
    {
        List<UpgradeSO> candidates = new List<UpgradeSO>();
        // Shared-pool filtering: skip nulls, already-applied upgrades, and duplicate asset references.
        for (int i = 0; i < upgradePool.Count; i++)
        {
            UpgradeSO upgrade = upgradePool[i];
            if (upgrade != null && !unit.HasAppliedUpgrade(upgrade) && !candidates.Contains(upgrade))
            {
                candidates.Add(upgrade);
            }
        }

        List<UpgradeSO> offer = new List<UpgradeSO>();
        int choiceCount = Mathf.Min(Mathf.Max(0, upgradeChoiceCount), candidates.Count);

        // Remove each picked candidate so a single offer never contains duplicates.
        while (offer.Count < choiceCount)
        {
            int candidateIndex = Random.Range(0, candidates.Count);
            offer.Add(candidates[candidateIndex]);
            candidates.RemoveAt(candidateIndex);
        }

        return offer;
    }

    private bool SelectPendingUpgrade(string unitId, UpgradeSO upgrade)
    {
        if (!pendingOffers.TryGetValue(unitId, out List<UpgradeSO> offer))
        {
            return false;
        }

        pendingOffers.Remove(unitId);

        if (RecordSelection(unitId, upgrade))
        {
            return true;
        }

        pendingOffers[unitId] = offer;
        return false;
    }

    private bool RecordSelection(string unitId, UpgradeSO upgrade)
    {
        if (unitStateManager == null || !unitStateManager.RecordSelectedUpgrade(unitId, upgrade))
        {
            return false;
        }

        ResolveReferences();
        if (eventBus != null && unitStateManager.TryGetUnit(unitId, out UnitStateManager.OwnedUnitState unit))
        {
            eventBus.RaiseUnitUpgradeSelected(new UnitUpgradeSelectedEvent(
                unitId,
                upgrade,
                unit.Level,
                unit.Experience,
                unit.HasNextExperienceThreshold,
                unit.NextExperienceThreshold));
        }

        return true;
    }

    private void ResolveReferences()
    {
        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }

        if (unitStateManager == null)
        {
            unitStateManager = FindAnyObjectByType<UnitStateManager>();
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
            ResolveReferences();
        }

        if (eventBus == null)
        {
            return;
        }

        eventBus.UnitUpgradeThresholdReached += HandleUnitUpgradeThresholdReached;
        eventBus.UnitUpgradeChoiceRequested += HandleUnitUpgradeChoiceRequested;
        eventBusSubscribed = true;
    }

    private void UnsubscribeFromEventBus()
    {
        if (!eventBusSubscribed || eventBus == null)
        {
            return;
        }

        eventBus.UnitUpgradeThresholdReached -= HandleUnitUpgradeThresholdReached;
        eventBus.UnitUpgradeChoiceRequested -= HandleUnitUpgradeChoiceRequested;
        eventBusSubscribed = false;
    }
}
