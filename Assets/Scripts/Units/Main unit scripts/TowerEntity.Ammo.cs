using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime ammo state, service-locator event access, and primary-weapon ammo consumption for <see cref="TowerEntity"/>.
/// </summary>
public partial class TowerEntity
{
    private static readonly Dictionary<string, TowerEntity> activeTowerIds = new Dictionary<string, TowerEntity>();

    private int currentAmmoUnits;
    private int maxAmmoUnits;
    private bool ammoInitialized;
    private UnitEventBus eventBus;
    private UnitProgression unitProgression;
    private string unitId;
    private bool usesGeneratedRuntimeUnitId;

    public int CurrentAmmoUnits => currentAmmoUnits;
    public int MaxAmmoUnits => maxAmmoUnits;
    public float AmmoEffectiveness => Mathf.Max(0f, GetStat(ENTITY_STATS.AmmoEffectiveness));
    public bool HasResolvedUnitId => !string.IsNullOrWhiteSpace(unitId);

    private void OnDestroy()
    {
        ReleaseRegisteredUnitId(true);
    }

    private void InitializeAmmoState()
    {
        maxAmmoUnits = GetCompiledAmmoUnits();
        currentAmmoUnits = maxAmmoUnits;
        ammoInitialized = true;
    }

    private void ResetAmmoStateForPreview()
    {
        currentAmmoUnits = 0;
        maxAmmoUnits = GetCompiledAmmoUnits();
        ammoInitialized = false;
    }

    private void RefreshAmmoCapacityFromStats()
    {
        int compiledAmmoUnits = GetCompiledAmmoUnits();
        if (!ammoInitialized)
        {
            maxAmmoUnits = compiledAmmoUnits;
            return;
        }

        int previousMaxAmmoUnits = maxAmmoUnits;
        maxAmmoUnits = compiledAmmoUnits;

        if (maxAmmoUnits > previousMaxAmmoUnits)
        {
            currentAmmoUnits = Mathf.Min(maxAmmoUnits, currentAmmoUnits + (maxAmmoUnits - previousMaxAmmoUnits));
            return;
        }

        if (currentAmmoUnits > maxAmmoUnits)
        {
            currentAmmoUnits = maxAmmoUnits;
        }
    }

    private int GetCompiledAmmoUnits()
    {
        return Mathf.Max(0, Mathf.FloorToInt(GetStat(ENTITY_STATS.AmmoUnits)));
    }

    private bool CanStartPrimaryAttack(AttackBehaviour primaryAttackBehaviour)
    {
        return primaryAttackBehaviour == null
            || !primaryAttackBehaviour.UsesFiniteAmmo
            || currentAmmoUnits > 0;
    }

    internal void HandlePrimaryAttackAmmoConsumed(AttackBehaviour attackBehaviour, int amountConsumed)
    {
        if (attackBehaviour == null
            || amountConsumed <= 0
            || !attackBehaviour.UsesFiniteAmmo
            || string.IsNullOrWhiteSpace(unitId))
        {
            return;
        }

        int previousAmmoUnits = currentAmmoUnits;
        currentAmmoUnits = Mathf.Max(0, currentAmmoUnits - amountConsumed);

        int actualConsumed = previousAmmoUnits - currentAmmoUnits;
        if (actualConsumed <= 0)
        {
            return;
        }

        UnitEventBus resolvedEventBus = ResolveEventBus();
        if (resolvedEventBus == null)
        {
            return;
        }

        resolvedEventBus.RaiseUnitAmmoConsumed(new UnitAmmoConsumedEvent(
            unitId,
            this,
            attackBehaviour,
            actualConsumed,
            currentAmmoUnits,
            maxAmmoUnits));
    }

    private UnitEventBus ResolveEventBus()
    {
        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }

        return eventBus;
    }

    private bool ValidateOrResolveUnitId()
    {
        ResolveUnitProgression();

        string progressionUnitId = unitProgression != null ? unitProgression.UnitId : null;
        if (!string.IsNullOrWhiteSpace(progressionUnitId))
        {
            RegisterRosterManagedUnitId(progressionUnitId);
            return !string.IsNullOrWhiteSpace(unitId);
        }

        if (!string.IsNullOrWhiteSpace(unitId) && IsRegisteredToThisTower(unitId))
        {
            return true;
        }

        string generatedUnitId = GenerateUniqueRuntimeUnitId();
        RegisterGeneratedUnitId(generatedUnitId);
        return !string.IsNullOrWhiteSpace(unitId);
    }

    private void ResolveUnitProgression()
    {
        if (unitProgression == null)
        {
            unitProgression = GetComponent<UnitProgression>();
        }

        if (unitProgression == null)
        {
            unitProgression = GetComponentInChildren<UnitProgression>(true);
        }
    }

    private void RegisterRosterManagedUnitId(string rosterUnitId)
    {
        if (unitId == rosterUnitId && IsRegisteredToThisTower(rosterUnitId))
        {
            usesGeneratedRuntimeUnitId = false;
            return;
        }

        ReleaseRegisteredUnitId(false);

        if (activeTowerIds.TryGetValue(rosterUnitId, out TowerEntity existingTower)
            && existingTower != null
            && existingTower != this)
        {
            Debug.LogWarning(
                $"{nameof(TowerEntity)} '{name}' deployed with duplicate roster unitId '{rosterUnitId}'. Keeping the roster identity without rewriting it.",
                this);
            unitId = rosterUnitId;
            usesGeneratedRuntimeUnitId = false;
            AssignResolvedUnitIdToProgression(rosterUnitId);
            return;
        }

        activeTowerIds[rosterUnitId] = this;
        unitId = rosterUnitId;
        usesGeneratedRuntimeUnitId = false;
        AssignResolvedUnitIdToProgression(rosterUnitId);
    }

    private void RegisterGeneratedUnitId(string generatedUnitId)
    {
        ReleaseRegisteredUnitId(false);
        activeTowerIds[generatedUnitId] = this;
        unitId = generatedUnitId;
        usesGeneratedRuntimeUnitId = true;
        AssignResolvedUnitIdToProgression(generatedUnitId);
    }

    private void ReleaseRegisteredUnitId(bool clearResolvedUnitId)
    {
        if (!string.IsNullOrWhiteSpace(unitId)
            && activeTowerIds.TryGetValue(unitId, out TowerEntity existingTower)
            && existingTower == this)
        {
            activeTowerIds.Remove(unitId);
        }

        if (clearResolvedUnitId)
        {
            unitId = string.Empty;

            if (usesGeneratedRuntimeUnitId && unitProgression != null)
            {
                unitProgression.AssignRuntimeUnitId(string.Empty);
            }
        }

        usesGeneratedRuntimeUnitId = false;
    }

    private void ReleaseResolvedUnitIdForPreview()
    {
        ReleaseRegisteredUnitId(true);
    }

    private bool IsRegisteredToThisTower(string candidateUnitId)
    {
        return !string.IsNullOrWhiteSpace(candidateUnitId)
            && activeTowerIds.TryGetValue(candidateUnitId, out TowerEntity existingTower)
            && existingTower == this;
    }

    private static string GenerateUniqueRuntimeUnitId()
    {
        string generatedUnitId;
        do
        {
            generatedUnitId = Guid.NewGuid().ToString("N");
        }
        while (activeTowerIds.ContainsKey(generatedUnitId));

        return generatedUnitId;
    }

    private void AssignResolvedUnitIdToProgression(string resolvedUnitId)
    {
        if (unitProgression != null)
        {
            unitProgression.AssignRuntimeUnitId(resolvedUnitId);
        }
    }
}
