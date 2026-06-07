using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Details-panel upgrade button that opens the selected roster unit's pending upgrade offer.
/// </summary>
public sealed class UnitDetailsUpgradeButton : MonoBehaviour
{
    [SerializeField, Tooltip("Button players press to open the selected unit's pending upgrade choices.")]
    private Button upgradeButton;

    [SerializeField, Tooltip("Optional root hidden when no roster-managed unit is selected. Defaults to the upgrade button object.")]
    private GameObject upgradeButtonRoot;

    [SerializeField, Tooltip("Player state source used to read the selected unit. Uses the scene service locator when empty.")]
    private PlayerStateController playerStateController;

    [SerializeField, Tooltip("Roster state source used to check pending upgrade state. Uses the scene service locator when empty.")]
    private UnitStateManager unitStateManager;

    [SerializeField, Tooltip("Event bus used to request the selected unit's pending upgrade offer. Uses the scene service locator when empty.")]
    private UnitEventBus eventBus;

    [SerializeField, Range(0f, 1f), Tooltip("Alpha used while the selected roster unit has no pending upgrade.")]
    private float unavailableAlpha = 0.35f;

    private CanvasGroup selfVisibilityGroup;
    private Coroutine delayedRefreshCoroutine;
    private Color originalDisabledColor;
    private float originalDisabledColorMultiplier;
    private bool subscribedToButton;
    private bool subscribedToPlayerState;
    private bool subscribedToEventBus;
    private bool cachedOriginalButtonColors;

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

    private void OnDisable()
    {
        StopDelayedRefresh();
        Unsubscribe();
    }

    private void OnDestroy()
    {
        StopDelayedRefresh();
        Unsubscribe();
    }

    private void OnValidate()
    {
        unavailableAlpha = Mathf.Clamp01(unavailableAlpha);
        ResolveReferences();
    }

    private void HandleUpgradeClicked()
    {
        ResolveReferences();

        if (!TryGetSelectedManagedUnitId(out string unitId) || !HasPendingUpgrade(unitId))
        {
            RefreshState();
            return;
        }

        if (eventBus == null)
        {
            Debug.LogWarning($"{nameof(UnitDetailsUpgradeButton)} cannot request upgrade choices because no {nameof(UnitEventBus)} is assigned.", this);
            return;
        }

        eventBus.RaiseUnitUpgradeOfferRequested(new UnitUpgradeOfferRequestedEvent(unitId));
    }

    private void HandleSelectionChanged(PlayerSelectionChangedEvent eventData)
    {
        RefreshState();
    }

    private void HandleUnitUpgradeThresholdReached(UnitUpgradeThresholdReachedEvent eventData)
    {
        if (IsSelectedUnit(eventData.UnitId))
        {
            RefreshStateNextFrame();
        }
    }

    private void HandleUnitUpgradeChoicesOffered(UnitUpgradeChoicesOfferedEvent eventData)
    {
        if (IsSelectedUnit(eventData.UnitId))
        {
            RefreshState();
        }
    }

    private void HandleUnitUpgradeSelected(UnitUpgradeSelectedEvent eventData)
    {
        if (IsSelectedUnit(eventData.UnitId))
        {
            RefreshState();
        }
    }

    private void HandleUnitDeployed(UnitDeployedEvent eventData)
    {
        if (IsSelectedUnit(eventData.UnitId))
        {
            RefreshState();
        }
    }

    private void HandleUnitRecalled(UnitRecalledEvent eventData)
    {
        if (IsSelectedUnit(eventData.UnitId))
        {
            RefreshState();
        }
    }

    private void RefreshState()
    {
        ResolveReferences();

        bool hasManagedSelection = TryGetSelectedManagedUnitId(out string unitId);
        bool hasPendingUpgrade = hasManagedSelection && HasPendingUpgrade(unitId);

        SetUpgradeVisible(hasManagedSelection);

        if (upgradeButton != null)
        {
            ApplyButtonAvailabilityColor(hasPendingUpgrade);
            upgradeButton.interactable = hasPendingUpgrade;
        }
    }

    private void RefreshStateNextFrame()
    {
        if (!isActiveAndEnabled || !Application.isPlaying)
        {
            RefreshState();
            return;
        }

        StopDelayedRefresh();
        delayedRefreshCoroutine = StartCoroutine(RefreshStateNextFrameRoutine());
    }

    private IEnumerator RefreshStateNextFrameRoutine()
    {
        yield return null;
        delayedRefreshCoroutine = null;
        RefreshState();
    }

    private void StopDelayedRefresh()
    {
        if (delayedRefreshCoroutine == null)
        {
            return;
        }

        StopCoroutine(delayedRefreshCoroutine);
        delayedRefreshCoroutine = null;
    }

    private bool TryGetSelectedManagedUnitId(out string unitId)
    {
        unitId = playerStateController != null ? playerStateController.SelectedUnitId : null;
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return false;
        }

        if (unitStateManager == null)
        {
            ServiceLocator.TryResolve(out unitStateManager);
        }

        return unitStateManager != null && unitStateManager.TryGetUnit(unitId, out _);
    }

    private bool HasPendingUpgrade(string unitId)
    {
        return unitStateManager != null
            && !string.IsNullOrWhiteSpace(unitId)
            && unitStateManager.HasPendingUpgradeSelection(unitId);
    }

    private bool IsSelectedUnit(string unitId)
    {
        return playerStateController != null
            && !string.IsNullOrWhiteSpace(unitId)
            && playerStateController.SelectedUnitId == unitId;
    }

    private void ResolveReferences()
    {
        if (upgradeButton == null)
        {
            upgradeButton = GetComponentInChildren<Button>(true);
        }

        if (upgradeButtonRoot == null && upgradeButton != null)
        {
            upgradeButtonRoot = upgradeButton.gameObject;
        }

        if (playerStateController == null)
        {
            ServiceLocator.TryResolve(out playerStateController);
        }

        if (unitStateManager == null)
        {
            ServiceLocator.TryResolve(out unitStateManager);
        }

        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }
    }

    private void Subscribe()
    {
        SubscribeToButton();
        SubscribeToPlayerState();
        SubscribeToEventBus();
    }

    private void Unsubscribe()
    {
        UnsubscribeFromButton();
        UnsubscribeFromPlayerState();
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

    private void SubscribeToPlayerState()
    {
        if (subscribedToPlayerState)
        {
            return;
        }

        if (playerStateController == null)
        {
            ResolveReferences();
        }

        if (playerStateController == null)
        {
            return;
        }

        playerStateController.SelectionChanged += HandleSelectionChanged;
        subscribedToPlayerState = true;
    }

    private void UnsubscribeFromPlayerState()
    {
        if (!subscribedToPlayerState || playerStateController == null)
        {
            return;
        }

        playerStateController.SelectionChanged -= HandleSelectionChanged;
        subscribedToPlayerState = false;
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

        eventBus.UnitUpgradeThresholdReached += HandleUnitUpgradeThresholdReached;
        eventBus.UnitUpgradeChoicesOffered += HandleUnitUpgradeChoicesOffered;
        eventBus.UnitUpgradeSelected += HandleUnitUpgradeSelected;
        eventBus.UnitDeployed += HandleUnitDeployed;
        eventBus.UnitRecalled += HandleUnitRecalled;
        subscribedToEventBus = true;
    }

    private void UnsubscribeFromEventBus()
    {
        if (!subscribedToEventBus || eventBus == null)
        {
            return;
        }

        eventBus.UnitUpgradeThresholdReached -= HandleUnitUpgradeThresholdReached;
        eventBus.UnitUpgradeChoicesOffered -= HandleUnitUpgradeChoicesOffered;
        eventBus.UnitUpgradeSelected -= HandleUnitUpgradeSelected;
        eventBus.UnitDeployed -= HandleUnitDeployed;
        eventBus.UnitRecalled -= HandleUnitRecalled;
        subscribedToEventBus = false;
    }

    private void SetUpgradeVisible(bool isVisible)
    {
        GameObject target = GetVisibilityTarget();
        if (target == null)
        {
            return;
        }

        if (target == gameObject)
        {
            SetSelfVisible(isVisible);
            return;
        }

        if (target.activeSelf != isVisible)
        {
            target.SetActive(isVisible);
        }
    }

    private GameObject GetVisibilityTarget()
    {
        return upgradeButtonRoot != null
            ? upgradeButtonRoot
            : upgradeButton != null ? upgradeButton.gameObject : null;
    }

    private void SetSelfVisible(bool isVisible)
    {
        if (selfVisibilityGroup == null)
        {
            selfVisibilityGroup = GetComponent<CanvasGroup>();
            if (selfVisibilityGroup == null && Application.isPlaying)
            {
                selfVisibilityGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (selfVisibilityGroup == null)
        {
            return;
        }

        selfVisibilityGroup.alpha = isVisible ? 1f : 0f;
        selfVisibilityGroup.interactable = isVisible;
        selfVisibilityGroup.blocksRaycasts = isVisible;
    }

    private void ApplyButtonAvailabilityColor(bool hasPendingUpgrade)
    {
        if (upgradeButton == null)
        {
            return;
        }

        CacheOriginalButtonColors();

        ColorBlock colors = upgradeButton.colors;
        if (hasPendingUpgrade)
        {
            colors.disabledColor = originalDisabledColor;
            colors.colorMultiplier = originalDisabledColorMultiplier;
        }
        else
        {
            Color unavailableColor = colors.normalColor;
            unavailableColor.a *= unavailableAlpha;
            colors.disabledColor = unavailableColor;
        }

        upgradeButton.colors = colors;
    }

    private void CacheOriginalButtonColors()
    {
        if (cachedOriginalButtonColors || upgradeButton == null)
        {
            return;
        }

        ColorBlock colors = upgradeButton.colors;
        originalDisabledColor = colors.disabledColor;
        originalDisabledColorMultiplier = colors.colorMultiplier;
        cachedOriginalButtonColors = true;
    }
}
