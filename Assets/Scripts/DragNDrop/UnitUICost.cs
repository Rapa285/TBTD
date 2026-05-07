using TMPro;
using UnityEngine;

/// <summary>
/// Displays the cached deployment cost for one roster-managed unit item.
/// </summary>
[RequireComponent(typeof(UIUnitItem))]
public class UnitUICost : MonoBehaviour
{
    [SerializeField, Tooltip("Sibling unit item model used to resolve roster identity and cost.")]
    private UIUnitItem uiUnitItem;

    [SerializeField, Tooltip("TMP text used to display the deployment cost.")]
    private TMP_Text costText;

    [SerializeField, Tooltip("Optional root shown only when a deployment cost is available. Defaults to the text object.")]
    private GameObject costRoot;

    [SerializeField, Tooltip("Text color used when the unit is affordable or currency is not being enforced.")]
    private Color affordableColor = Color.white;

    [SerializeField, Tooltip("Text color used when the current currency is lower than this unit's cost.")]
    private Color unaffordableColor = Color.red;

    private UnitEventBus eventBus;
    private CurrencyManager currencyManager;
    private bool eventBusSubscribed;

    private void Awake()
    {
        ResolveReferences();
        ClearDisplay();
    }

    private void Start()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        SubscribeToEventBus();
        RefreshDisplay();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (!Application.isPlaying)
        {
            return;
        }

        SubscribeToEventBus();
        RefreshDisplay();
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            ClearDisplay();
        }

        UnsubscribeFromEventBus();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEventBus();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void HandleCurrencyChanged(CurrencyChangedEvent eventData)
    {
        RefreshDisplay();
    }

    private void HandleUnitDeploymentCostCompiled(UnitDeploymentCostCompiledEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            RefreshDisplay();
        }
    }

    private void HandleUnitDeployed(UnitDeployedEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            RefreshDisplay();
        }
    }

    private void HandleUnitRecalled(UnitRecalledEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            RefreshDisplay();
        }
    }

    private void ResolveReferences()
    {
        if (uiUnitItem == null)
        {
            uiUnitItem = GetComponent<UIUnitItem>();
        }

        if (costText == null)
        {
            costText = GetComponentInChildren<TMP_Text>(true);
        }

        if (costRoot == null && costText != null)
        {
            costRoot = costText.gameObject;
        }

        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
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

        eventBus.CurrencyChanged += HandleCurrencyChanged;
        eventBus.UnitDeploymentCostCompiled += HandleUnitDeploymentCostCompiled;
        eventBus.UnitDeployed += HandleUnitDeployed;
        eventBus.UnitRecalled += HandleUnitRecalled;
        eventBusSubscribed = true;
    }

    private void UnsubscribeFromEventBus()
    {
        if (!eventBusSubscribed || eventBus == null)
        {
            return;
        }

        eventBus.CurrencyChanged -= HandleCurrencyChanged;
        eventBus.UnitDeploymentCostCompiled -= HandleUnitDeploymentCostCompiled;
        eventBus.UnitDeployed -= HandleUnitDeployed;
        eventBus.UnitRecalled -= HandleUnitRecalled;
        eventBusSubscribed = false;
    }

    private void RefreshDisplay()
    {
        ResolveReferences();

        if (!TryGetDisplayCost(out int cost))
        {
            ClearDisplay();
            return;
        }

        SetCostVisible(true);

        if (costText != null)
        {
            costText.text = cost.ToString();
            costText.color = IsAffordable(cost) ? affordableColor : unaffordableColor;
            costText.enabled = true;
        }
    }

    private bool TryGetDisplayCost(out int cost)
    {
        cost = 0;

        if (uiUnitItem == null || !uiUnitItem.IsManagedUnitConfigured)
        {
            return false;
        }

        if (!uiUnitItem.TryGetOwnedUnit(out UnitStateManager.OwnedUnitState unit) || unit.IsDeployed)
        {
            return false;
        }

        UnitStateManager stateManager = uiUnitItem.UnitStateManager;
        return stateManager != null && stateManager.TryGetDeploymentCost(uiUnitItem.UnitId, out cost);
    }

    private bool IsAffordable(int cost)
    {
        if (currencyManager == null)
        {
            ResolveReferences();
        }

        return currencyManager == null || currencyManager.CanAfford(cost);
    }

    private bool IsMatchingUnit(string unitId)
    {
        return uiUnitItem != null
            && uiUnitItem.IsManagedUnit
            && uiUnitItem.UnitId == unitId;
    }

    private void ClearDisplay()
    {
        SetCostVisible(false);

        if (costText != null)
        {
            costText.text = string.Empty;
            costText.enabled = false;
        }
    }

    private void SetCostVisible(bool isVisible)
    {
        GameObject target = costRoot != null
            ? costRoot
            : costText != null ? costText.gameObject : null;

        if (target != null && target != gameObject && target.activeSelf != isVisible)
        {
            target.SetActive(isVisible);
        }
    }
}
