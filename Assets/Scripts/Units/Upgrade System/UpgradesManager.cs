using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates shared-pool multi-upgrade offers and records selected upgrade-line levels for roster units.
/// </summary>
[DefaultExecutionOrder(-800)]
public class UpgradesManager : MonoBehaviour
{
    private sealed class PendingUpgradeOffer
    {
        public int Seed { get; }
        public List<UnitUpgradeOfferChoice> Choices { get; }

        public PendingUpgradeOffer(int seed, List<UnitUpgradeOfferChoice> choices)
        {
            Seed = seed;
            Choices = choices;
        }
    }

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

    [SerializeField, Min(0), Tooltip("Currency cost for the first upgrade reroll.")]
    private int baseRerollCost = 10;

    [SerializeField, Min(0), Tooltip("Amount added to the shared reroll cost after each successful reroll.")]
    private int rerollCostIncrement = 5;

    [SerializeField, Min(0), Tooltip("Shared free upgrade rerolls available when this manager initializes.")]
    private int startingFreeRerolls = 3;

    private readonly Dictionary<string, PendingUpgradeOffer> pendingOffers = new Dictionary<string, PendingUpgradeOffer>();
    private readonly System.Random seedGenerator = new System.Random();
    private CurrencyManager currencyManager;
    private string activeMenuUnitId;
    private int freeRerollsRemaining;
    private int paidRerollCount;
    private bool eventBusSubscribed;

    public IReadOnlyList<EvolutionSO> EvolutionPool => evolutionPool;
    public int FreeRerollsRemaining => Mathf.Max(0, freeRerollsRemaining);
    public bool HasFreeRerolls => FreeRerollsRemaining > 0;
    public int CurrentRerollCost => HasFreeRerolls ? 0 : CurrentPaidRerollCost;

    private int CurrentPaidRerollCost => Mathf.Max(0, baseRerollCost)
        + Mathf.Max(0, paidRerollCount) * Mathf.Max(0, rerollCostIncrement);

    private void Awake()
    {
        freeRerollsRemaining = Mathf.Max(0, startingFreeRerolls);
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

    private void OnValidate()
    {
        baseRerollCost = Mathf.Max(0, baseRerollCost);
        rerollCostIncrement = Mathf.Max(0, rerollCostIncrement);
        startingFreeRerolls = Mathf.Max(0, startingFreeRerolls);
        upgradeChoiceCount = Mathf.Max(0, upgradeChoiceCount);
    }

    /// <summary>
    /// Grants shared free upgrade reroll credits for future reward systems.
    /// </summary>
    public void GrantFreeRerolls(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount == 0)
        {
            return;
        }

        freeRerollsRemaining = FreeRerollsRemaining + amount;
        RaiseRerollStateChanged();
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
        if (pendingOffers.TryGetValue(unitId, out PendingUpgradeOffer offer))
        {
            choices = offer.Choices;
            return true;
        }

        choices = null;
        return false;
    }

    /// <summary>
    /// Gets the remembered random seed for one unit's current pending offer.
    /// </summary>
    public bool TryGetPendingOfferSeed(string unitId, out int seed)
    {
        if (pendingOffers.TryGetValue(unitId, out PendingUpgradeOffer offer))
        {
            seed = offer.Seed;
            return true;
        }

        seed = 0;
        return false;
    }

    /// <summary>
    /// Returns whether the current candidate pool can produce an alternate offer for this pending unit.
    /// </summary>
    public bool CanRerollPendingOffer(string unitId)
    {
        ResolveReferences();
        return unitStateManager != null
            && pendingOffers.TryGetValue(unitId, out PendingUpgradeOffer offer)
            && unitStateManager.TryGetUnit(unitId, out UnitStateManager.OwnedUnitState unit)
            && CanBuildAlternateOffer(BuildCandidates(unit), offer.Choices);
    }

    /// <summary>
    /// Selects one pending upgrade choice by UI index from the currently stored offer.
    /// </summary>
    public bool SelectUpgrade(string unitId, int choiceIndex)
    {
        if (!pendingOffers.TryGetValue(unitId, out PendingUpgradeOffer offer)
            || choiceIndex < 0
            || choiceIndex >= offer.Choices.Count)
        {
            return false;
        }

        return SelectPendingUpgrade(unitId, offer.Choices[choiceIndex]);
    }

    /// <summary>
    /// Selects one pending multi-upgrade choice by asset reference from the currently stored offer.
    /// </summary>
    public bool SelectUpgrade(string unitId, MultiUpgradeSO upgrade)
    {
        if (upgrade == null
            || !pendingOffers.TryGetValue(unitId, out PendingUpgradeOffer offer))
        {
            return false;
        }

        for (int i = 0; i < offer.Choices.Count; i++)
        {
            if (offer.Choices[i].MultiUpgrade == upgrade)
            {
                return SelectPendingUpgrade(unitId, offer.Choices[i]);
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
            || !pendingOffers.TryGetValue(unitId, out PendingUpgradeOffer offer))
        {
            return false;
        }

        for (int i = 0; i < offer.Choices.Count; i++)
        {
            if (offer.Choices[i].Evolution == evolution)
            {
                return SelectPendingUpgrade(unitId, offer.Choices[i]);
            }
        }

        return false;
    }

    private void HandleUnitUpgradeOfferRequested(UnitUpgradeOfferRequestedEvent eventData)
    {
        if (string.IsNullOrWhiteSpace(eventData.UnitId))
        {
            return;
        }

        EnsurePendingOffer(eventData.UnitId);
        RaisePendingOffer(eventData.UnitId);
    }

    private void HandleUnitUpgradeChoiceRequested(UnitUpgradeChoiceRequestedEvent eventData)
    {
        if (string.IsNullOrWhiteSpace(eventData.UnitId))
        {
            return;
        }

        SelectUpgrade(eventData.UnitId, eventData.ChoiceIndex);
    }

    private void HandleUnitUpgradeRerollRequested(UnitUpgradeRerollRequestedEvent eventData)
    {
        if (string.IsNullOrWhiteSpace(eventData.UnitId))
        {
            return;
        }

        RerollPendingOffer(eventData.UnitId);
    }

    private void HandleUnitUpgradeMenuClosed(UnitUpgradeMenuClosedEvent eventData)
    {
        if (string.IsNullOrWhiteSpace(eventData.UnitId) || eventData.UnitId == activeMenuUnitId)
        {
            activeMenuUnitId = null;
        }
    }

    private bool RerollPendingOffer(string unitId)
    {
        ResolveReferences();
        if (unitStateManager == null
            || !pendingOffers.TryGetValue(unitId, out PendingUpgradeOffer currentOffer)
            || !unitStateManager.TryGetUnit(unitId, out UnitStateManager.OwnedUnitState unit)
            || !TryBuildRerolledOffer(unit, currentOffer, out PendingUpgradeOffer rerolledOffer))
        {
            return false;
        }

        if (HasFreeRerolls)
        {
            freeRerollsRemaining = FreeRerollsRemaining - 1;
        }
        else
        {
            int rerollCost = CurrentPaidRerollCost;
            if (currencyManager != null && !currencyManager.TrySpend(rerollCost))
            {
                return false;
            }

            paidRerollCount++;
        }

        pendingOffers[unitId] = rerolledOffer;
        RaisePendingOffer(unitId);
        RaiseRerollStateChanged();
        return true;
    }

    private void CreateStoredOfferOrAdvance(string unitId, UnitStateManager.OwnedUnitState unit)
    {
        CreateStoredOfferOrConsumeInvalidCredits(unitId, true);
    }

    private bool EnsurePendingOffer(string unitId)
    {
        ResolveReferences();
        if (pendingOffers.ContainsKey(unitId))
        {
            return true;
        }

        if (unitStateManager == null
            || !unitStateManager.HasPendingUpgradeSelection(unitId)
            || !unitStateManager.TryGetUnit(unitId, out UnitStateManager.OwnedUnitState unit))
        {
            return false;
        }

        CreateStoredOfferOrConsumeInvalidCredits(unitId, true);
        return pendingOffers.ContainsKey(unitId);
    }

    private bool CreatePendingOffer(string unitId, UnitStateManager.OwnedUnitState unit)
    {
        List<UnitUpgradeOfferChoice> candidates = BuildCandidates(unit);
        int seed = NextOfferSeed();
        List<UnitUpgradeOfferChoice> offer = BuildOffer(candidates, seed);
        if (offer.Count == 0)
        {
            return false;
        }

        pendingOffers[unitId] = new PendingUpgradeOffer(seed, offer);
        return true;
    }

    private bool TryBuildRerolledOffer(
        UnitStateManager.OwnedUnitState unit,
        PendingUpgradeOffer currentOffer,
        out PendingUpgradeOffer rerolledOffer)
    {
        rerolledOffer = null;
        List<UnitUpgradeOfferChoice> candidates = BuildCandidates(unit);
        if (!CanBuildAlternateOffer(candidates, currentOffer.Choices))
        {
            return false;
        }

        for (int i = 0; i < 128; i++)
        {
            int seed = NextOfferSeed();
            List<UnitUpgradeOfferChoice> choices = BuildOffer(candidates, seed);
            if (!OffersMatch(currentOffer.Choices, choices))
            {
                rerolledOffer = new PendingUpgradeOffer(seed, choices);
                return true;
            }
        }

        return false;
    }

    private List<UnitUpgradeOfferChoice> BuildCandidates(UnitStateManager.OwnedUnitState unit)
    {
        List<UnitUpgradeOfferChoice> candidates = new List<UnitUpgradeOfferChoice>();
        if (unit == null)
        {
            return candidates;
        }

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

        return candidates;
    }

    private List<UnitUpgradeOfferChoice> BuildOffer(IReadOnlyList<UnitUpgradeOfferChoice> candidates, int seed)
    {
        List<UnitUpgradeOfferChoice> remainingCandidates = new List<UnitUpgradeOfferChoice>(candidates);
        List<UnitUpgradeOfferChoice> offer = new List<UnitUpgradeOfferChoice>();
        int choiceCount = Mathf.Min(Mathf.Max(0, upgradeChoiceCount), remainingCandidates.Count);
        System.Random random = new System.Random(seed);

        // Remove each picked candidate so a single offer never contains duplicates.
        while (offer.Count < choiceCount)
        {
            int candidateIndex = random.Next(remainingCandidates.Count);
            offer.Add(remainingCandidates[candidateIndex]);
            remainingCandidates.RemoveAt(candidateIndex);
        }

        return offer;
    }

    private bool SelectPendingUpgrade(string unitId, UnitUpgradeOfferChoice choice)
    {
        if (!pendingOffers.ContainsKey(unitId))
        {
            return false;
        }

        bool shouldKeepMenuOpen = activeMenuUnitId == unitId;
        PendingUpgradeOffer removedOffer = pendingOffers[unitId];
        pendingOffers.Remove(unitId);

        if (RecordSelection(unitId, choice))
        {
            if (shouldKeepMenuOpen && pendingOffers.ContainsKey(unitId))
            {
                RaisePendingOffer(unitId);
            }
            else if (shouldKeepMenuOpen)
            {
                activeMenuUnitId = null;
            }

            return true;
        }

        pendingOffers[unitId] = removedOffer;
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

        CreateStoredOfferOrConsumeInvalidCredits(unitId, false);

        ResolveReferences();
        if (eventBus != null && unitStateManager.TryGetUnit(unitId, out UnitStateManager.OwnedUnitState unit))
        {
            bool hasNextExperienceThreshold = unitStateManager.TryGetNextExperienceThreshold(
                unitId,
                out float nextExperienceThreshold);

            eventBus.RaiseUnitUpgradeSelected(new UnitUpgradeSelectedEvent(
                unitId,
                selectedMultiUpgrade,
                selectedUpgrade,
                selectedUpgradeLevel,
                unit.Level,
                unit.Experience,
                hasNextExperienceThreshold,
                nextExperienceThreshold,
                selectedEvolution,
                unit.UnspentUpgradeCount));
        }

        return true;
    }

    private bool CreateStoredOfferOrConsumeInvalidCredits(string unitId, bool raiseConsumedCreditEvents)
    {
        ResolveReferences();
        if (unitStateManager == null || string.IsNullOrWhiteSpace(unitId))
        {
            return false;
        }

        while (unitStateManager.TryGetUnit(unitId, out UnitStateManager.OwnedUnitState unit)
            && unit.UnspentUpgradeCount > 0)
        {
            if (pendingOffers.ContainsKey(unitId) || CreatePendingOffer(unitId, unit))
            {
                return true;
            }

            if (!unitStateManager.RecordSelectedUpgrade(
                unitId,
                null,
                null,
                out UpgradeSO selectedUpgrade,
                out int selectedUpgradeLevel))
            {
                return false;
            }

            if (raiseConsumedCreditEvents)
            {
                RaiseUpgradeSelectedEvent(unitId, null, null, selectedUpgrade, selectedUpgradeLevel);
            }
        }

        return false;
    }

    private void RaiseUpgradeSelectedEvent(
        string unitId,
        MultiUpgradeSO selectedMultiUpgrade,
        EvolutionSO selectedEvolution,
        UpgradeSO selectedUpgrade,
        int selectedUpgradeLevel)
    {
        ResolveReferences();
        if (eventBus == null
            || unitStateManager == null
            || !unitStateManager.TryGetUnit(unitId, out UnitStateManager.OwnedUnitState unit))
        {
            return;
        }

        bool hasNextExperienceThreshold = unitStateManager.TryGetNextExperienceThreshold(
            unitId,
            out float nextExperienceThreshold);

        eventBus.RaiseUnitUpgradeSelected(new UnitUpgradeSelectedEvent(
            unitId,
            selectedMultiUpgrade,
            selectedUpgrade,
            selectedUpgradeLevel,
            unit.Level,
            unit.Experience,
            hasNextExperienceThreshold,
            nextExperienceThreshold,
            selectedEvolution,
            unit.UnspentUpgradeCount));
    }

    private bool RaisePendingOffer(string unitId)
    {
        ResolveReferences();
        if (!pendingOffers.TryGetValue(unitId, out PendingUpgradeOffer offer)
            || offer.Choices.Count == 0
            || eventBus == null)
        {
            return false;
        }

        activeMenuUnitId = unitId;
        eventBus.RaiseUnitUpgradeChoicesOffered(new UnitUpgradeChoicesOfferedEvent(unitId, offer.Choices.ToArray()));
        return true;
    }

    private void RaiseRerollStateChanged()
    {
        ResolveReferences();
        if (eventBus != null)
        {
            eventBus.RaiseUpgradeRerollStateChanged(new UpgradeRerollStateChangedEvent(
                FreeRerollsRemaining,
                CurrentRerollCost));
        }
    }

    private int NextOfferSeed()
    {
        return seedGenerator.Next(1, int.MaxValue);
    }

    private bool CanBuildAlternateOffer(
        IReadOnlyList<UnitUpgradeOfferChoice> candidates,
        IReadOnlyList<UnitUpgradeOfferChoice> currentOffer)
    {
        int choiceCount = Mathf.Min(Mathf.Max(0, upgradeChoiceCount), candidates.Count);
        return choiceCount > 0
            && currentOffer != null
            && currentOffer.Count > 0
            && candidates.Count > choiceCount;
    }

    private static bool OffersMatch(
        IReadOnlyList<UnitUpgradeOfferChoice> left,
        IReadOnlyList<UnitUpgradeOfferChoice> right)
    {
        if (left == null || right == null || left.Count != right.Count)
        {
            return false;
        }

        for (int i = 0; i < left.Count; i++)
        {
            if (!ChoicesMatch(left[i], right[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ChoicesMatch(UnitUpgradeOfferChoice left, UnitUpgradeOfferChoice right)
    {
        return left.MultiUpgrade == right.MultiUpgrade
            && left.Evolution == right.Evolution;
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

        if (currencyManager == null)
        {
            ServiceLocator.TryResolve(out currencyManager);
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

        eventBus.UnitUpgradeOfferRequested += HandleUnitUpgradeOfferRequested;
        eventBus.UnitUpgradeChoiceRequested += HandleUnitUpgradeChoiceRequested;
        eventBus.UnitUpgradeRerollRequested += HandleUnitUpgradeRerollRequested;
        eventBus.UnitUpgradeMenuClosed += HandleUnitUpgradeMenuClosed;
        eventBusSubscribed = true;
    }

    private void UnsubscribeFromEventBus()
    {
        if (!eventBusSubscribed || eventBus == null)
        {
            return;
        }

        eventBus.UnitUpgradeOfferRequested -= HandleUnitUpgradeOfferRequested;
        eventBus.UnitUpgradeChoiceRequested -= HandleUnitUpgradeChoiceRequested;
        eventBus.UnitUpgradeRerollRequested -= HandleUnitUpgradeRerollRequested;
        eventBus.UnitUpgradeMenuClosed -= HandleUnitUpgradeMenuClosed;
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
