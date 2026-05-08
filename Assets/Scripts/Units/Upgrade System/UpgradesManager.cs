using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates shared-pool multi-upgrade offers and records selected upgrade-line levels for roster units.
/// </summary>
[DefaultExecutionOrder(-800)]
public class UpgradesManager : MonoBehaviour
{
    [SerializeField, Tooltip("Event bus used to listen for level-up requests and publish offers/selections.")]
    private UnitEventBus eventBus;

    [SerializeField, Tooltip("Roster manager that owns unit progression state and applied multi-upgrade levels.")]
    private UnitStateManager unitStateManager;

    [SerializeField, Tooltip("Shared pool of multi-upgrade lines that every current unit can be offered.")]
    private List<MultiUpgradeSO> upgradePool = new List<MultiUpgradeSO>();

    [SerializeField, Tooltip("Shared pool of weapon evolutions offered when their prerequisites are met.")]
    private List<EvolutionSO> evolutionPool = new List<EvolutionSO>();

    [SerializeField, Min(0), Tooltip("Maximum number of unique choices generated for one offer.")]
    private int upgradeChoiceCount = 3;

    private readonly Dictionary<string, List<UnitUpgradeOfferChoice>> pendingOffers = new Dictionary<string, List<UnitUpgradeOfferChoice>>();
    private bool eventBusSubscribed;

    private void Awake()
    {
        RegisterWithServiceLocator();
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

    private void OnDestroy()
    {
        ServiceLocator.Unregister<UpgradesManager>(this);
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
    public bool TryGetPendingChoices(string unitId, out IReadOnlyList<UnitUpgradeOfferChoice> choices)
    {
        if (pendingOffers.TryGetValue(unitId, out List<UnitUpgradeOfferChoice> offer))
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
        if (!pendingOffers.TryGetValue(unitId, out List<UnitUpgradeOfferChoice> offer)
            || choiceIndex < 0
            || choiceIndex >= offer.Count)
        {
            return false;
        }

        return SelectPendingUpgrade(unitId, offer[choiceIndex]);
    }

    /// <summary>
    /// Selects one pending multi-upgrade choice by asset reference from the currently stored offer.
    /// </summary>
    public bool SelectUpgrade(string unitId, MultiUpgradeSO upgrade)
    {
        if (upgrade == null
            || !pendingOffers.TryGetValue(unitId, out List<UnitUpgradeOfferChoice> offer))
        {
            return false;
        }

        for (int i = 0; i < offer.Count; i++)
        {
            if (offer[i].MultiUpgrade == upgrade)
            {
                return SelectPendingUpgrade(unitId, offer[i]);
            }
        }

        return false;
    }

    /// <summary>
    /// Selects one pending evolution choice by asset reference from the currently stored offer.
    /// </summary>
    public bool SelectUpgrade(string unitId, EvolutionSO evolution)
    {
        if (evolution == null
            || !pendingOffers.TryGetValue(unitId, out List<UnitUpgradeOfferChoice> offer))
        {
            return false;
        }

        for (int i = 0; i < offer.Count; i++)
        {
            if (offer[i].Evolution == evolution)
            {
                return SelectPendingUpgrade(unitId, offer[i]);
            }
        }

        return false;
    }

    private void HandleUnitUpgradeThresholdReached(UnitUpgradeThresholdReachedEvent eventData)
    {
        ResolveReferences();

        string unitId = eventData.UnitId;
        if (string.IsNullOrWhiteSpace(unitId)
            || unitStateManager == null
            || pendingOffers.ContainsKey(unitId)
            || !unitStateManager.TryGetUnit(unitId, out UnitStateManager.OwnedUnitState unit)
            || !unitStateManager.TryBeginUpgradeSelection(unitId))
        {
            return;
        }

        List<UnitUpgradeOfferChoice> offer = BuildOffer(unit);
        if (offer.Count == 0)
        {
            RecordSelection(unitId, default);
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

    private List<UnitUpgradeOfferChoice> BuildOffer(UnitStateManager.OwnedUnitState unit)
    {
        List<UnitUpgradeOfferChoice> candidates = new List<UnitUpgradeOfferChoice>();
        // Shared-pool filtering: skip nulls, maxed lines, invalid next levels, and duplicate asset references.
        for (int i = 0; i < upgradePool.Count; i++)
        {
            MultiUpgradeSO upgrade = upgradePool[i];
            if (upgrade == null || ContainsMultiUpgrade(candidates, upgrade))
            {
                continue;
            }

            if (unit.TryGetNextMultiUpgradeLevel(
                upgrade,
                out int currentLevel,
                out int nextLevel,
                out UpgradeSO resolvedUpgrade))
            {
                candidates.Add(new UnitUpgradeOfferChoice(
                    upgrade,
                    resolvedUpgrade,
                    currentLevel,
                    nextLevel,
                    upgrade.MaxLevel));
            }
        }

        for (int i = 0; i < evolutionPool.Count; i++)
        {
            EvolutionSO evolution = evolutionPool[i];
            if (evolution == null
                || ContainsEvolution(candidates, evolution)
                || !unit.CanSelectEvolution(evolution))
            {
                continue;
            }

            candidates.Add(new UnitUpgradeOfferChoice(evolution));
        }

        List<UnitUpgradeOfferChoice> offer = new List<UnitUpgradeOfferChoice>();
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

    private bool SelectPendingUpgrade(string unitId, UnitUpgradeOfferChoice choice)
    {
        if (!pendingOffers.TryGetValue(unitId, out List<UnitUpgradeOfferChoice> offer))
        {
            return false;
        }

        pendingOffers.Remove(unitId);

        if (RecordSelection(unitId, choice))
        {
            return true;
        }

        pendingOffers[unitId] = offer;
        return false;
    }

    private bool RecordSelection(string unitId, UnitUpgradeOfferChoice choice)
    {
        ResolveReferences();

        MultiUpgradeSO selectedMultiUpgrade = choice.MultiUpgrade;
        EvolutionSO selectedEvolution = choice.Evolution;
        if (unitStateManager == null
            || !unitStateManager.RecordSelectedUpgrade(
                unitId,
                selectedMultiUpgrade,
                selectedEvolution,
                out UpgradeSO selectedUpgrade,
                out int selectedUpgradeLevel))
        {
            return false;
        }

        ResolveReferences();
        if (eventBus != null && unitStateManager.TryGetUnit(unitId, out UnitStateManager.OwnedUnitState unit))
        {
            eventBus.RaiseUnitUpgradeSelected(new UnitUpgradeSelectedEvent(
                unitId,
                selectedMultiUpgrade,
                selectedUpgrade,
                selectedUpgradeLevel,
                unit.Level,
                unit.Experience,
                unit.HasNextExperienceThreshold,
                unit.NextExperienceThreshold,
                selectedEvolution));
        }

        return true;
    }

    private static bool ContainsMultiUpgrade(IReadOnlyList<UnitUpgradeOfferChoice> choices, MultiUpgradeSO upgrade)
    {
        for (int i = 0; i < choices.Count; i++)
        {
            if (choices[i].MultiUpgrade == upgrade)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsEvolution(IReadOnlyList<UnitUpgradeOfferChoice> choices, EvolutionSO evolution)
    {
        for (int i = 0; i < choices.Count; i++)
        {
            if (choices[i].Evolution == evolution)
            {
                return true;
            }
        }

        return false;
    }

    private void ResolveReferences()
    {
        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }

        if (unitStateManager == null)
        {
            ServiceLocator.TryResolve(out unitStateManager);
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

    private void RegisterWithServiceLocator()
    {
        if (ServiceLocator.TryResolve<UpgradesManager>(out UpgradesManager existingUpgradesManager)
            && existingUpgradesManager != null
            && existingUpgradesManager != this)
        {
            Debug.LogWarning(
                $"{nameof(UpgradesManager)} on '{name}' replaced the previously registered {nameof(UpgradesManager)} on '{existingUpgradesManager.name}'.",
                this);
        }

        ServiceLocator.Register<UpgradesManager>(this);
    }
}
