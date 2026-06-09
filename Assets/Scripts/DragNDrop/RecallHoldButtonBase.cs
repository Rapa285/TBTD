using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Shared hold-to-recall state machine for UI and world-space recall controls.
/// </summary>
public abstract class RecallHoldButtonBase : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IRecallPointerReceiver
{
    [SerializeField, Min(0f), Tooltip("Seconds the recall control must be held before recall completes.")]
    private float holdDuration = 1.5f;

    [SerializeField, Tooltip("Optional scene-level recall animation controller. If empty, the scene service locator is used.")]
    private RecallAnimController recallAnimController;

    private UnitEventBus eventBus;
    private bool subscribedToEventBus;
    private bool isHoldingRecall;
    private float holdElapsed;
    private string activeUnitId;
    private TowerEntity activeRecallTower;
    private UnitStateManager activeStateManager;

    protected bool IsHoldingRecall => isHoldingRecall;
    protected float HoldDuration => holdDuration;

    protected virtual void Awake()
    {
        ResolveReferences();
    }

    protected virtual void Start()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Subscribe();
        RefreshState();
    }

    protected virtual void OnEnable()
    {
        ResolveReferences();

        if (!Application.isPlaying)
        {
            return;
        }

        Subscribe();
        RefreshState();
    }

    protected virtual void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!subscribedToEventBus)
        {
            SubscribeToEventBus();
        }

        if (!isHoldingRecall)
        {
            return;
        }

        if (!CanContinueRecallHold())
        {
            CancelRecallHold();
            return;
        }

        AdvanceRecallHold();
    }

    protected virtual void OnDisable()
    {
        if (Application.isPlaying)
        {
            CancelRecallHold();
            SetRecallAvailable(false);
            HandlePointerHoverChanged(false);
        }

        Unsubscribe();
    }

    protected virtual void OnDestroy()
    {
        Unsubscribe();
    }

    protected virtual void OnValidate()
    {
        ResolveReferences();
        holdDuration = Mathf.Max(0f, holdDuration);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsRecallVisible() && IsRecallInteractable())
        {
            HandlePointerHoverChanged(true);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        HandleRecallPointerDown(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        HandleRecallPointerUp(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HandleRecallPointerExit(eventData);
    }

    public void HandleRecallPointerDown(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        TryBeginRecallHold();
    }

    public void HandleRecallPointerUp(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        CancelRecallHold();
    }

    public void HandleRecallPointerExit(PointerEventData eventData)
    {
        HandlePointerHoverChanged(false);
        CancelRecallHold();
        RefreshState();
    }

    protected void RefreshState()
    {
        ResolveReferences();

        bool isAvailable = TryGetRecallTarget(out string unitId, out UnitStateManager stateManager, out TowerEntity tower)
            && IsValidRecallTarget(unitId, stateManager, tower);

        SetRecallAvailable(isAvailable);
    }

    protected virtual void ResolveReferences()
    {
        if (recallAnimController == null)
        {
            ServiceLocator.TryResolve(out recallAnimController);
        }

        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }
    }

    protected virtual void Subscribe()
    {
        SubscribeToEventBus();
    }

    protected virtual void Unsubscribe()
    {
        UnsubscribeFromEventBus();
    }

    protected abstract bool TryGetRecallTarget(out string unitId, out UnitStateManager stateManager, out TowerEntity tower);

    protected virtual bool ShouldRefreshForUnit(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return false;
        }

        if (isHoldingRecall && activeUnitId == unitId)
        {
            return true;
        }

        return TryGetRecallTarget(out string currentUnitId, out _, out _)
            && currentUnitId == unitId;
    }

    protected virtual void SetRecallAvailable(bool isAvailable)
    {
        if (!isAvailable)
        {
            CancelRecallHold();
            HandlePointerHoverChanged(false);
        }
    }

    protected virtual bool IsRecallVisible()
    {
        return isActiveAndEnabled;
    }

    protected virtual bool IsRecallInteractable()
    {
        return true;
    }

    protected virtual void HandlePointerHoverChanged(bool isHovered)
    {
    }

    protected virtual void HandleHoldStarted(float duration)
    {
    }

    protected virtual void HandleHoldProgress(float normalizedProgress)
    {
    }

    protected virtual void HandleHoldCancelled()
    {
    }

    protected virtual void HandleHoldCompleted()
    {
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

    private void HandleUnitDeployed(UnitDeployedEvent eventData)
    {
        if (ShouldRefreshForUnit(eventData.UnitId))
        {
            RefreshState();
        }
    }

    private void HandleUnitRecalled(UnitRecalledEvent eventData)
    {
        if (ShouldRefreshForUnit(eventData.UnitId))
        {
            CancelRecallHold();
            RefreshState();
        }
    }

    protected void TryBeginRecallHold()
    {
        if (isHoldingRecall
            || !TryGetRecallTarget(out string unitId, out UnitStateManager stateManager, out TowerEntity tower)
            || !CanStartRecallHold(unitId, stateManager, tower))
        {
            return;
        }

        activeUnitId = unitId;
        activeStateManager = stateManager;
        activeRecallTower = tower;
        holdElapsed = 0f;
        isHoldingRecall = true;
        HandleHoldStarted(holdDuration);
        recallAnimController?.PlayRecallInProgress(tower, holdDuration);
        RaiseRecallStarted(unitId, tower);

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

        HandleHoldProgress(normalizedProgress);

        if (normalizedProgress >= 1f)
        {
            CompleteRecallHold();
        }
    }

    protected void CancelRecallHold()
    {
        if (!isHoldingRecall)
        {
            HandleHoldCancelled();
            return;
        }

        TowerEntity tower = activeRecallTower != null ? activeRecallTower : TryGetCurrentRecallTower();
        isHoldingRecall = false;
        holdElapsed = 0f;
        activeUnitId = null;
        activeStateManager = null;
        activeRecallTower = null;
        HandleHoldCancelled();
        recallAnimController?.CancelRecallInProgress(tower);
    }

    private void CompleteRecallHold()
    {
        if (!isHoldingRecall)
        {
            return;
        }

        string unitId = activeUnitId;
        UnitStateManager stateManager = activeStateManager;
        TowerEntity tower = activeRecallTower != null ? activeRecallTower : TryGetCurrentRecallTower();
        Vector3 recallPosition = tower != null ? tower.transform.position : transform.position;

        isHoldingRecall = false;
        holdElapsed = holdDuration;
        activeUnitId = null;
        activeStateManager = null;
        activeRecallTower = null;
        HandleHoldCompleted();
        recallAnimController?.CancelRecallInProgress(tower);

        bool didRecall = stateManager != null
            && !string.IsNullOrWhiteSpace(unitId)
            && stateManager.RecallUnit(unitId);

        if (didRecall)
        {
            if (recallAnimController != null)
            {
                recallAnimController.PlayRecallSuccessAtPosition(recallPosition, holdDuration);
            }
        }
        else
        {
            HandleHoldCancelled();
        }

        RefreshState();
    }

    private bool CanStartRecallHold(string unitId, UnitStateManager stateManager, TowerEntity tower)
    {
        return IsRecallVisible()
            && IsRecallInteractable()
            && IsValidRecallTarget(unitId, stateManager, tower);
    }

    private bool CanContinueRecallHold()
    {
        return TryGetRecallTarget(out string unitId, out UnitStateManager stateManager, out TowerEntity tower)
            && unitId == activeUnitId
            && stateManager == activeStateManager
            && tower == activeRecallTower
            && IsRecallVisible()
            && IsRecallInteractable()
            && IsValidRecallTarget(unitId, stateManager, tower);
    }

    private TowerEntity TryGetCurrentRecallTower()
    {
        return TryGetRecallTarget(out _, out _, out TowerEntity tower)
            ? tower
            : null;
    }

    private bool IsValidRecallTarget(string unitId, UnitStateManager stateManager, TowerEntity tower)
    {
        if (string.IsNullOrWhiteSpace(unitId) || stateManager == null || tower == null || !tower.Deployed)
        {
            return false;
        }

        if (!stateManager.TryGetUnit(unitId, out UnitStateManager.OwnedUnitState unit))
        {
            return false;
        }

        return unit.IsDeployed && unit.CurrentRuntimeInstance == tower;
    }

    private void RaiseRecallStarted(string unitId, TowerEntity tower)
    {
        if (tower == null || string.IsNullOrWhiteSpace(unitId))
        {
            return;
        }

        if (eventBus == null)
        {
            ResolveReferences();
        }

        if (eventBus != null)
        {
            eventBus.RaiseUnitRecallStarted(new UnitRecallStartedEvent(unitId, tower));
        }
    }
}
