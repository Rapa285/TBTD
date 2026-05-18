using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI recall behavior for one managed unit item.
/// </summary>
[RequireComponent(typeof(UIUnitItem))]
public class UnitUIRecall : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField, Tooltip("Sibling unit item model used to resolve roster identity and deployment state.")]
    private UIUnitItem uiUnitItem;

    [SerializeField, Tooltip("Sibling deployment behavior used to read hover state.")]
    private UnitUIDeployment unitUiDeployment;

    [SerializeField, Tooltip("Recall button shown when a managed unit is deployed and this item is hovered.")]
    private Button recallButton;

    [SerializeField, Tooltip("Optional root shown and hidden with recall visibility. Defaults to the recall button object.")]
    private GameObject recallButtonRoot;

    [SerializeField, Min(0f), Tooltip("Seconds the recall button must be held before recall completes.")]
    private float holdDuration = 1.5f;

    [SerializeField, Tooltip("Optional visual fill controller for the recall hold progress.")]
    private RecallButtonFX recallButtonFX;

    [SerializeField, Tooltip("Optional scene-level recall animation controller. If empty, the scene service locator is used.")]
    private RecallAnimController recallAnimController;

    private UnitEventBus eventBus;
    private bool subscribedToEventBus;
    private bool wasHovered;
    private bool wasDeployed;
    private bool isHoldingRecall;
    private float holdElapsed;
    private TowerEntity activeRecallTower;
    private RecallPointerForwarder recallPointerForwarder;

    private void Awake()
    {
        ResolveReferences();
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

        bool isHovered = unitUiDeployment != null && unitUiDeployment.IsHovered;
        bool isDeployed = uiUnitItem != null && uiUnitItem.IsDeployed;
        if (isHovered != wasHovered || isDeployed != wasDeployed)
        {
            RefreshState();
        }

        if (isHoldingRecall)
        {
            if (!CanContinueRecallHold())
            {
                CancelRecallHold();
                return;
            }

            AdvanceRecallHold();
        }
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            CancelRecallHold();
            SetRecallVisible(false);
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
        holdDuration = Mathf.Max(0f, holdDuration);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        TryBeginRecallHold();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        CancelRecallHold();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RefreshState();
    }

    private void HandleUnitDeployed(UnitDeployedEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            CancelRecallHold();
            RefreshState();
        }
    }

    private void HandleUnitRecalled(UnitRecalledEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            CancelRecallHold();
            RefreshState();
        }
    }

    private void ResolveReferences()
    {
        if (uiUnitItem == null)
        {
            uiUnitItem = GetComponent<UIUnitItem>();
        }

        if (unitUiDeployment == null)
        {
            unitUiDeployment = GetComponent<UnitUIDeployment>();
        }

        if (recallButtonRoot == null && recallButton != null)
        {
            recallButtonRoot = recallButton.gameObject;
        }

        if (recallButtonFX == null)
        {
            recallButtonFX = GetComponentInChildren<RecallButtonFX>(true);
        }

        if (recallAnimController == null)
        {
            ServiceLocator.TryResolve(out recallAnimController);
        }

        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }

        EnsurePointerForwarder();
    }

    private void Subscribe()
    {
        SubscribeToEventBus();
    }

    private void Unsubscribe()
    {
        UnsubscribeFromEventBus();
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

        eventBus.UnitDeployed -= HandleUnitDeployed;
        eventBus.UnitRecalled -= HandleUnitRecalled;
        subscribedToEventBus = false;
    }

    private void RefreshState()
    {
        ResolveReferences();

        bool isHovered = unitUiDeployment != null && unitUiDeployment.IsHovered;
        bool isDeployed = uiUnitItem != null && uiUnitItem.IsDeployed;
        wasHovered = isHovered;
        wasDeployed = isDeployed;

        bool shouldShowRecall = uiUnitItem != null
            && uiUnitItem.IsManagedUnitConfigured
            && isDeployed
            && isHovered;

        SetRecallVisible(shouldShowRecall);
    }

    private bool IsMatchingUnit(string unitId)
    {
        return uiUnitItem != null
            && uiUnitItem.IsManagedUnit
            && uiUnitItem.UnitId == unitId;
    }

    private void SetRecallVisible(bool isVisible)
    {
        GameObject target = recallButtonRoot != null
            ? recallButtonRoot
            : recallButton != null ? recallButton.gameObject : null;

        if (!isVisible)
        {
            CancelRecallHold();
        }

        if (target != null && target.activeSelf != isVisible)
        {
            target.SetActive(isVisible);
        }
    }

    private void TryBeginRecallHold()
    {
        if (isHoldingRecall || !CanStartRecallHold(out TowerEntity tower))
        {
            return;
        }

        activeRecallTower = tower;
        holdElapsed = 0f;
        isHoldingRecall = true;
        recallButtonFX?.BeginHold(holdDuration);
        recallAnimController?.PlayRecallInProgress(tower, holdDuration);

        if (holdDuration <= 0f)
        {
            CompleteRecallHold();
        }
    }

    private void AdvanceRecallHold()
    {
        holdElapsed += Time.deltaTime;
        float normalizedProgress = holdDuration > 0f
            ? Mathf.Clamp01(holdElapsed / holdDuration)
            : 1f;

        recallButtonFX?.SetFill(normalizedProgress);

        if (normalizedProgress >= 1f)
        {
            CompleteRecallHold();
        }
    }

    private void CancelRecallHold()
    {
        if (!isHoldingRecall)
        {
            recallButtonFX?.ResetFill();
            return;
        }

        TowerEntity tower = activeRecallTower != null ? activeRecallTower : TryGetCurrentRuntimeTower();
        isHoldingRecall = false;
        holdElapsed = 0f;
        activeRecallTower = null;
        recallButtonFX?.CancelHold();
        recallAnimController?.CancelRecallInProgress(tower);
    }

    private void CompleteRecallHold()
    {
        if (!isHoldingRecall)
        {
            return;
        }

        TowerEntity tower = activeRecallTower != null ? activeRecallTower : TryGetCurrentRuntimeTower();
        Vector3 recallPosition = tower != null ? tower.transform.position : transform.position;

        isHoldingRecall = false;
        holdElapsed = holdDuration;
        activeRecallTower = null;
        recallButtonFX?.CompleteHold();
        recallAnimController?.CancelRecallInProgress(tower);

        bool didRecall = uiUnitItem != null && uiUnitItem.TryRecall();
        if (didRecall)
        {
            if (recallAnimController != null)
            {
                recallAnimController.PlayRecallSuccessAtPosition(recallPosition, holdDuration);
            }
        }
        else
        {
            recallButtonFX?.ResetFill();
        }

        RefreshState();
    }

    private bool CanStartRecallHold(out TowerEntity tower)
    {
        ResolveReferences();
        tower = TryGetCurrentRuntimeTower();

        return uiUnitItem != null
            && uiUnitItem.IsManagedUnitConfigured
            && uiUnitItem.IsDeployed
            && unitUiDeployment != null
            && unitUiDeployment.IsHovered
            && tower != null
            && IsRecallVisible()
            && (recallButton == null || recallButton.IsInteractable());
    }

    private bool CanContinueRecallHold()
    {
        TowerEntity currentTower = TryGetCurrentRuntimeTower();

        return uiUnitItem != null
            && uiUnitItem.IsManagedUnitConfigured
            && uiUnitItem.IsDeployed
            && currentTower != null
            && activeRecallTower == currentTower
            && IsRecallVisible()
            && (recallButton == null || recallButton.IsInteractable());
    }

    private TowerEntity TryGetCurrentRuntimeTower()
    {
        return uiUnitItem != null
            && uiUnitItem.TryGetOwnedUnit(out UnitStateManager.OwnedUnitState unit)
            ? unit.CurrentRuntimeInstance
            : null;
    }

    private bool IsRecallVisible()
    {
        GameObject target = recallButtonRoot != null
            ? recallButtonRoot
            : recallButton != null ? recallButton.gameObject : null;

        return target != null && target.activeInHierarchy;
    }

    private void EnsurePointerForwarder()
    {
        if (recallButton == null || recallButton.gameObject == gameObject)
        {
            return;
        }

        if (recallPointerForwarder == null || recallPointerForwarder.gameObject != recallButton.gameObject)
        {
            recallPointerForwarder = recallButton.GetComponent<RecallPointerForwarder>();
            if (Application.isPlaying && recallPointerForwarder == null)
            {
                recallPointerForwarder = recallButton.gameObject.AddComponent<RecallPointerForwarder>();
            }
        }

        if (recallPointerForwarder != null)
        {
            recallPointerForwarder.Initialize(this);
        }
    }
}
