using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Roster-item upgrade button that opens a stored pending upgrade offer for one managed unit.
/// </summary>
[RequireComponent(typeof(UIUnitItem))]
public class UnitUIUpgrade : MonoBehaviour
{
    [SerializeField, Tooltip("Sibling unit item model used to resolve roster identity and upgrade state.")]
    private UIUnitItem uiUnitItem;

    [SerializeField, Tooltip("Button players press to open this unit's pending upgrade choices.")]
    private Button upgradeButton;

    [SerializeField, Tooltip("Optional root shown only while this unit has a pending upgrade. Defaults to the upgrade button object.")]
    private GameObject upgradeButtonRoot;

    private UnitEventBus eventBus;
    private bool subscribedToButton;
    private bool subscribedToEventBus;
    private bool wasUpgradePending;

    private void Awake()
    {
        ResolveReferences();
        RefreshState();
    }

    private void Start()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Subscribe();
        RefreshState();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (!Application.isPlaying)
        {
            return;
        }

        Subscribe();
        RefreshState();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!subscribedToEventBus)
        {
            SubscribeToEventBus();
        }

        bool upgradePending = HasPendingUpgrade();
        if (upgradePending != wasUpgradePending)
        {
            RefreshState();
        }
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            SetUpgradeVisible(false);
        }

        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void HandleUpgradeClicked()
    {
        ResolveReferences();
        if (!HasPendingUpgrade())
        {
            RefreshState();
            return;
        }

        if (eventBus == null)
        {
            Debug.LogWarning($"{nameof(UnitUIUpgrade)} cannot request upgrade choices because no {nameof(UnitEventBus)} is assigned.", this);
            return;
        }

        eventBus.RaiseUnitUpgradeOfferRequested(new UnitUpgradeOfferRequestedEvent(uiUnitItem.UnitId));
    }

    private void HandleUpgradeSelected(UnitUpgradeSelectedEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            RefreshState();
        }
    }

    private void HandleUpgradeChoicesOffered(UnitUpgradeChoicesOfferedEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            RefreshState();
        }
    }

    private void ResolveReferences()
    {
        if (uiUnitItem == null)
        {
            uiUnitItem = GetComponent<UIUnitItem>();
        }

        if (upgradeButton == null)
        {
            upgradeButton = GetComponentInChildren<Button>(true);
        }

        if (upgradeButtonRoot == null && upgradeButton != null)
        {
            upgradeButtonRoot = upgradeButton.gameObject;
        }

        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }
    }

    private void Subscribe()
    {
        SubscribeToButton();
        SubscribeToEventBus();
    }

    private void Unsubscribe()
    {
        UnsubscribeFromButton();
        UnsubscribeFromEventBus();
    }

    private void SubscribeToButton()
    {
        if (subscribedToButton || upgradeButton == null)
        {
            return;
        }

        upgradeButton.onClick.AddListener(HandleUpgradeClicked);
        subscribedToButton = true;
    }

    private void UnsubscribeFromButton()
    {
        if (!subscribedToButton || upgradeButton == null)
        {
            return;
        }

        upgradeButton.onClick.RemoveListener(HandleUpgradeClicked);
        subscribedToButton = false;
    }

    private void SubscribeToEventBus()
    {
        if (subscribedToEventBus)
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

        eventBus.UnitUpgradeChoicesOffered += HandleUpgradeChoicesOffered;
        eventBus.UnitUpgradeSelected += HandleUpgradeSelected;
        subscribedToEventBus = true;
    }

    private void UnsubscribeFromEventBus()
    {
        if (!subscribedToEventBus || eventBus == null)
        {
            return;
        }

        eventBus.UnitUpgradeChoicesOffered -= HandleUpgradeChoicesOffered;
        eventBus.UnitUpgradeSelected -= HandleUpgradeSelected;
        subscribedToEventBus = false;
    }

    private void RefreshState()
    {
        ResolveReferences();

        bool upgradePending = HasPendingUpgrade();
        wasUpgradePending = upgradePending;

        SetUpgradeVisible(upgradePending);
        if (upgradeButton != null)
        {
            upgradeButton.interactable = upgradePending;
        }
    }

    private bool HasPendingUpgrade()
    {
        if (uiUnitItem == null || !uiUnitItem.IsManagedUnitConfigured)
        {
            return false;
        }

        UnitStateManager stateManager = uiUnitItem.UnitStateManager;
        return stateManager != null && stateManager.HasPendingUpgradeSelection(uiUnitItem.UnitId);
    }

    private bool IsMatchingUnit(string unitId)
    {
        return uiUnitItem != null
            && uiUnitItem.IsManagedUnit
            && uiUnitItem.UnitId == unitId;
    }

    private void SetUpgradeVisible(bool isVisible)
    {
        GameObject target = upgradeButtonRoot != null
            ? upgradeButtonRoot
            : upgradeButton != null ? upgradeButton.gameObject : null;

        if (target != null && target.activeSelf != isVisible)
        {
            target.SetActive(isVisible);
        }
    }
}
