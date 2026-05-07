using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public enum PlayerInteractionState
{
    None = 0,
    TowerSelected = 1,
    DeploymentPreview = 2,
    UpgradeMenu = 3,
    Paused = 4,
    GameOver = 5
}

public readonly struct PlayerSelectionChangedEvent
{
    public TowerEntity PreviousTower { get; }
    public TowerEntity CurrentTower { get; }
    public string PreviousUnitId { get; }
    public string CurrentUnitId { get; }

    public PlayerSelectionChangedEvent(
        TowerEntity previousTower,
        TowerEntity currentTower,
        string previousUnitId,
        string currentUnitId)
    {
        PreviousTower = previousTower;
        CurrentTower = currentTower;
        PreviousUnitId = previousUnitId;
        CurrentUnitId = currentUnitId;
    }
}

public readonly struct PlayerInteractionStateChangedEvent
{
    public PlayerInteractionState PreviousState { get; }
    public PlayerInteractionState CurrentState { get; }

    public PlayerInteractionStateChangedEvent(
        PlayerInteractionState previousState,
        PlayerInteractionState currentState)
    {
        PreviousState = previousState;
        CurrentState = currentState;
    }
}

/// <summary>
/// Scene-level authority for player interaction state that affects UI and tower selection.
/// </summary>
[DefaultExecutionOrder(-825)]
public class PlayerStateController : MonoBehaviour
{
    [SerializeField, Tooltip("Event bus used to mirror deployment and upgrade UI modes.")]
    private UnitEventBus eventBus;

    [SerializeField, Tooltip("Deployment controller cancelled when entering pause or gameover.")]
    private UnitDeploymentController deploymentController;

    [SerializeField, Tooltip("PlayerInput used for click-driven selection. Falls back to the first scene PlayerInput.")]
    private PlayerInput playerInput;

    [SerializeField, Tooltip("Camera used for direct physics tower selection raycasts. Falls back to Camera.main or the first active camera.")]
    private Camera raycastCamera;

    [SerializeField, Tooltip("Layers considered valid tower selection targets. Include TowerUnit here and exclude TowerVision.")]
    private LayerMask selectionLayers = ~0;

    [SerializeField, Tooltip("Layers ignored by tower selection raycasts. Include TowerVision here.")]
    private LayerMask selectionPassThroughLayers;

    [SerializeField, Tooltip("Layers that block tower selection before a selectable tower is hit.")]
    private LayerMask selectionBlockingLayers;

    [SerializeField, Min(0.01f), Tooltip("Maximum distance used by tower selection raycasts.")]
    private float maxSelectionRayDistance = 1000f;

    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
    private TowerEntity selectedTower;
    private TowerSelectionTarget selectedSelectionTarget;
    private string selectedUnitId;
    private string activeUpgradeUnitId;
    private InputAction clickAction;
    private PointerEventData clickPointerEventData;
    private EventSystem clickPointerEventSystem;
    private PlayerInteractionState currentState;
    private bool isDeploymentPreviewActive;
    private bool isUpgradeMenuActive;
    private bool isPaused;
    private bool isGameOver;
    private bool eventBusSubscribed;
    private bool clickActionSubscribed;
    private bool warnedMissingClickAction;

    private enum ClickRaycastTarget
    {
        Empty = 0,
        SelectableTower = 1,
        NonSelectableWorld = 2
    }

    public PlayerInteractionState CurrentState => currentState;
    public TowerEntity SelectedTower => selectedTower;
    public TowerSelectionTarget SelectedSelectionTarget => selectedSelectionTarget;
    public string SelectedUnitId => selectedUnitId;
    public bool IsDeploymentPreviewActive => isDeploymentPreviewActive;
    public bool IsUpgradeMenuActive => isUpgradeMenuActive;
    public bool IsPaused => isPaused;
    public bool IsGameOver => isGameOver;

    public event Action<PlayerSelectionChangedEvent> SelectionChanged;
    public event Action<PlayerInteractionStateChangedEvent> InteractionStateChanged;

    private void Awake()
    {
        RegisterWithServiceLocator();
        ResolveReferences();
        RefreshInteractionState();
    }

    private void Start()
    {
        ResolveReferences();
        SubscribeToEventBus();
        SubscribeToClickAction();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToEventBus();
        SubscribeToClickAction();
    }

    private void OnDisable()
    {
        ClearSelection();
        UnsubscribeFromClickAction();
        UnsubscribeFromEventBus();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<PlayerStateController>(this);
    }

    public bool TrySelectTower(TowerEntity tower)
    {
        if (!CanSelectTowers() || tower == null || !tower.Deployed)
        {
            return false;
        }

        TowerSelectionTarget selectionTarget = ResolveSelectionTarget(tower);
        if (selectionTarget != null && !IsSelectionColliderAllowed(selectionTarget.SelectionCollider))
        {
            return false;
        }

        SetSelectedTower(tower, selectionTarget);
        return true;
    }

    public bool TrySelectTower(TowerSelectionTarget selectionTarget)
    {
        if (!CanSelectTowers()
            || selectionTarget == null
            || !IsSelectionColliderAllowed(selectionTarget.SelectionCollider)
            || !selectionTarget.TryGetSelectableTower(out TowerEntity tower))
        {
            return false;
        }

        SetSelectedTower(tower, selectionTarget);
        return true;
    }

    public void ClearSelection()
    {
        SetSelectedTower(null, null);
    }

    public bool IsSelected(TowerSelectionTarget selectionTarget)
    {
        return selectionTarget != null && selectedSelectionTarget == selectionTarget;
    }

    public bool IsSelectionColliderAllowed(Collider selectionCollider)
    {
        return selectionCollider != null && IsInSelectionLayer(selectionCollider.gameObject);
    }

    public bool CanStartDeploymentPreview()
    {
        return !isGameOver
            && !isPaused
            && !isUpgradeMenuActive
            && !isDeploymentPreviewActive;
    }

    public void SetPaused(bool value)
    {
        if (isPaused == value)
        {
            return;
        }

        isPaused = value;
        if (isPaused)
        {
            ClearSelection();
            CancelDeploymentPreviewIfNeeded();
        }

        RefreshInteractionState();
    }

    public void SetGameOver(bool value)
    {
        if (isGameOver == value)
        {
            return;
        }

        isGameOver = value;
        if (isGameOver)
        {
            ClearSelection();
            CancelDeploymentPreviewIfNeeded();
        }

        RefreshInteractionState();
    }

    private bool CanSelectTowers()
    {
        return !isGameOver
            && !isPaused
            && !isUpgradeMenuActive
            && !isDeploymentPreviewActive;
    }

    private void SetSelectedTower(TowerEntity tower, TowerSelectionTarget selectionTarget)
    {
        if (tower == null)
        {
            selectionTarget = null;
        }

        if (selectedTower == tower && selectedSelectionTarget == selectionTarget)
        {
            return;
        }

        TowerEntity previousTower = selectedTower;
        string previousUnitId = selectedUnitId;

        if (previousTower != null && previousTower != tower)
        {
            previousTower.SetSelected(false);
        }

        selectedTower = tower;
        selectedSelectionTarget = selectionTarget;
        selectedUnitId = tower != null ? tower.UnitId : null;

        if (selectedTower != null)
        {
            selectedTower.SetSelected(true);
        }

        SelectionChanged?.Invoke(new PlayerSelectionChangedEvent(
            previousTower,
            selectedTower,
            previousUnitId,
            selectedUnitId));

        RefreshInteractionState();
    }

    private void ClearInvalidSelection()
    {
        if (selectedTower != null && selectedTower.Deployed)
        {
            return;
        }

        ClearSelection();
    }

    private void HandleDeploymentPreviewStarted(UnitDeploymentPreviewStartedEvent eventData)
    {
        isDeploymentPreviewActive = true;
        ClearSelection();
        RefreshInteractionState();
    }

    private void HandleDeploymentPreviewEnded(UnitDeploymentPreviewEndedEvent eventData)
    {
        isDeploymentPreviewActive = false;
        RefreshInteractionState();
    }

    private void HandleUpgradeChoicesOffered(UnitUpgradeChoicesOfferedEvent eventData)
    {
        if (string.IsNullOrWhiteSpace(eventData.UnitId))
        {
            return;
        }

        activeUpgradeUnitId = eventData.UnitId;
        isUpgradeMenuActive = true;
        ClearSelection();
        RefreshInteractionState();
    }

    private void HandleUpgradeSelected(UnitUpgradeSelectedEvent eventData)
    {
        if (!string.IsNullOrWhiteSpace(activeUpgradeUnitId) && eventData.UnitId != activeUpgradeUnitId)
        {
            return;
        }

        activeUpgradeUnitId = null;
        isUpgradeMenuActive = false;
        RefreshInteractionState();
    }

    private void HandleUnitRecalled(UnitRecalledEvent eventData)
    {
        if (!string.IsNullOrWhiteSpace(selectedUnitId) && eventData.UnitId == selectedUnitId)
        {
            ClearSelection();
        }
    }

    private void CancelDeploymentPreviewIfNeeded()
    {
        if (deploymentController == null)
        {
            ResolveReferences();
        }

        if (!isDeploymentPreviewActive && (deploymentController == null || !deploymentController.IsDragging))
        {
            return;
        }

        if (deploymentController != null && deploymentController.IsDragging)
        {
            deploymentController.CancelDeployment();
        }
    }

    private void RefreshInteractionState()
    {
        PlayerInteractionState previousState = currentState;
        currentState = EvaluateInteractionState();

        if (previousState != currentState)
        {
            InteractionStateChanged?.Invoke(new PlayerInteractionStateChangedEvent(previousState, currentState));
        }
    }

    private PlayerInteractionState EvaluateInteractionState()
    {
        if (isGameOver)
        {
            return PlayerInteractionState.GameOver;
        }

        if (isPaused)
        {
            return PlayerInteractionState.Paused;
        }

        if (isUpgradeMenuActive)
        {
            return PlayerInteractionState.UpgradeMenu;
        }

        if (isDeploymentPreviewActive)
        {
            return PlayerInteractionState.DeploymentPreview;
        }

        return selectedTower != null
            ? PlayerInteractionState.TowerSelected
            : PlayerInteractionState.None;
    }

    private void ResolveReferences()
    {
        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }

        if (deploymentController == null)
        {
            ServiceLocator.TryResolve(out deploymentController);
        }

        ResolvePlayerInput();
        ResolveRaycastCamera();
    }

    private PlayerInput ResolvePlayerInput()
    {
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }

        if (playerInput == null)
        {
            playerInput = FindAnyObjectByType<PlayerInput>();
        }

        return playerInput;
    }

    private Camera ResolveRaycastCamera()
    {
        if (raycastCamera != null && raycastCamera.isActiveAndEnabled)
        {
            return raycastCamera;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.isActiveAndEnabled)
        {
            raycastCamera = mainCamera;
            return raycastCamera;
        }

        Camera[] sceneCameras = Camera.allCameras;
        for (int i = 0; i < sceneCameras.Length; i++)
        {
            Camera sceneCamera = sceneCameras[i];
            if (sceneCamera != null && sceneCamera.isActiveAndEnabled)
            {
                raycastCamera = sceneCamera;
                return raycastCamera;
            }
        }

        return raycastCamera;
    }

    private void SubscribeToClickAction()
    {
        if (clickActionSubscribed)
        {
            return;
        }

        if (playerInput == null)
        {
            ResolvePlayerInput();
        }

        clickAction = ResolveClickAction();
        if (clickAction == null)
        {
            if (!warnedMissingClickAction)
            {
                warnedMissingClickAction = true;
                Debug.LogWarning(
                    $"{nameof(PlayerStateController)} could not find a PlayerInput click action. Add an action named 'Click' or 'Attack', or a direct pointer press binding, to enable empty-space deselection.",
                    this);
            }

            return;
        }

        clickAction.performed += HandleClickPerformed;
        if (!clickAction.enabled)
        {
            clickAction.Enable();
        }

        clickActionSubscribed = true;
    }

    private void UnsubscribeFromClickAction()
    {
        if (!clickActionSubscribed || clickAction == null)
        {
            return;
        }

        clickAction.performed -= HandleClickPerformed;
        clickActionSubscribed = false;
        clickAction = null;
    }

    private InputAction ResolveClickAction()
    {
        if (playerInput == null || playerInput.actions == null)
        {
            return null;
        }

        InputAction action = playerInput.actions.FindAction("Click", false);
        if (action != null)
        {
            return action;
        }

        action = playerInput.actions.FindAction("Attack", false);
        if (action != null)
        {
            return action;
        }

        foreach (InputActionMap actionMap in playerInput.actions.actionMaps)
        {
            foreach (InputAction candidateAction in actionMap.actions)
            {
                foreach (InputBinding binding in candidateAction.bindings)
                {
                    if (IsDirectPointerPressBinding(binding))
                    {
                        return candidateAction;
                    }
                }
            }
        }

        return null;
    }

    private static bool IsDirectPointerPressBinding(InputBinding binding)
    {
        if (binding.isComposite || binding.isPartOfComposite || string.IsNullOrWhiteSpace(binding.path))
        {
            return false;
        }

        return binding.path.Contains("<Mouse>/leftButton")
            || binding.path.Contains("<Pointer>/press")
            || binding.path.Contains("<Touchscreen>/primaryTouch/press")
            || binding.path.Contains("<Touchscreen>/touch*/press")
            || binding.path.Contains("<Pen>/tip");
    }

    private void HandleClickPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed || !context.ReadValueAsButton())
        {
            return;
        }

        ResolveRaycastCamera();
        ClearInvalidSelection();

        if (!CanSelectTowers() || !TryGetPointerPosition(context, out Vector2 screenPosition))
        {
            return;
        }

        if (IsPointerOverBlockingUI(screenPosition))
        {
            return;
        }

        ClickRaycastTarget clickTarget = GetClickRaycastTarget(screenPosition, out TowerSelectionTarget selectionTarget);
        if (clickTarget == ClickRaycastTarget.SelectableTower)
        {
            TrySelectTower(selectionTarget);
            return;
        }

        if (clickTarget == ClickRaycastTarget.Empty || clickTarget == ClickRaycastTarget.NonSelectableWorld)
        {
            ClearSelection();
        }
    }

    private bool TryGetPointerPosition(InputAction.CallbackContext context, out Vector2 screenPosition)
    {
        if (context.control != null)
        {
            if (context.control.device is Pointer contextPointer)
            {
                screenPosition = contextPointer.position.ReadValue();
                return true;
            }

            if (context.control.device is Touchscreen contextTouchscreen)
            {
                screenPosition = contextTouchscreen.primaryTouch.position.ReadValue();
                return true;
            }

            screenPosition = default;
            return false;
        }

        if (Pointer.current != null)
        {
            screenPosition = Pointer.current.position.ReadValue();
            return true;
        }

        if (Mouse.current != null)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        if (Touchscreen.current != null)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        screenPosition = default;
        return false;
    }

    private bool IsPointerOverBlockingUI(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }

        if (clickPointerEventData == null || clickPointerEventSystem != eventSystem)
        {
            clickPointerEventData = new PointerEventData(eventSystem);
            clickPointerEventSystem = eventSystem;
        }

        clickPointerEventData.Reset();
        clickPointerEventData.position = screenPosition;
        clickPointerEventData.button = PointerEventData.InputButton.Left;
        uiRaycastResults.Clear();
        eventSystem.RaycastAll(clickPointerEventData, uiRaycastResults);

        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            RaycastResult result = uiRaycastResults[i];
            if (result.gameObject != null && result.module is not PhysicsRaycaster)
            {
                return true;
            }
        }

        return false;
    }

    private ClickRaycastTarget GetClickRaycastTarget(Vector2 screenPosition, out TowerSelectionTarget selectionTarget)
    {
        selectionTarget = null;

        Camera cameraToUse = ResolveRaycastCamera();
        if (cameraToUse == null)
        {
            return ClickRaycastTarget.Empty;
        }

        int raycastLayerMask = selectionLayers.value | selectionPassThroughLayers.value | selectionBlockingLayers.value;
        if (raycastLayerMask == 0)
        {
            return ClickRaycastTarget.Empty;
        }

        Ray ray = cameraToUse.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            maxSelectionRayDistance,
            raycastLayerMask,
            QueryTriggerInteraction.Collide);

        if (hits.Length == 0)
        {
            return ClickRaycastTarget.Empty;
        }

        Array.Sort(hits, CompareRaycastHitDistance);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
            {
                continue;
            }

            GameObject hitObject = hitCollider.gameObject;
            if (IsInLayer(hitObject, selectionPassThroughLayers))
            {
                continue;
            }

            if (IsInLayer(hitObject, selectionLayers))
            {
                TowerSelectionTarget target = hitCollider.GetComponentInParent<TowerSelectionTarget>();
                if (target != null && target.IsSelectionCollider(hitCollider) && target.TryGetSelectableTower(out _))
                {
                    selectionTarget = target;
                    return ClickRaycastTarget.SelectableTower;
                }

                return ClickRaycastTarget.NonSelectableWorld;
            }

            if (IsInLayer(hitObject, selectionBlockingLayers))
            {
                return ClickRaycastTarget.NonSelectableWorld;
            }
        }

        return ClickRaycastTarget.Empty;
    }

    private TowerSelectionTarget ResolveSelectionTarget(TowerEntity tower)
    {
        if (tower == null)
        {
            return null;
        }

        TowerSelectionTarget target = tower.GetComponent<TowerSelectionTarget>();
        if (target == null)
        {
            target = tower.GetComponentInChildren<TowerSelectionTarget>();
        }

        if (target == null)
        {
            target = tower.GetComponentInParent<TowerSelectionTarget>();
        }

        return target != null && target.TryGetSelectableTower(out TowerEntity selectableTower) && selectableTower == tower
            ? target
            : null;
    }

    private bool IsInSelectionLayer(GameObject target)
    {
        return IsInLayer(target, selectionLayers);
    }

    private static int CompareRaycastHitDistance(RaycastHit left, RaycastHit right)
    {
        return left.distance.CompareTo(right.distance);
    }

    private static bool IsInLayer(GameObject target, LayerMask layerMask)
    {
        return target != null && (layerMask.value & (1 << target.layer)) != 0;
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

        eventBus.UnitDeploymentPreviewStarted += HandleDeploymentPreviewStarted;
        eventBus.UnitDeploymentPreviewEnded += HandleDeploymentPreviewEnded;
        eventBus.UnitUpgradeChoicesOffered += HandleUpgradeChoicesOffered;
        eventBus.UnitUpgradeSelected += HandleUpgradeSelected;
        eventBus.UnitRecalled += HandleUnitRecalled;
        eventBusSubscribed = true;
    }

    private void UnsubscribeFromEventBus()
    {
        if (!eventBusSubscribed || eventBus == null)
        {
            return;
        }

        eventBus.UnitDeploymentPreviewStarted -= HandleDeploymentPreviewStarted;
        eventBus.UnitDeploymentPreviewEnded -= HandleDeploymentPreviewEnded;
        eventBus.UnitUpgradeChoicesOffered -= HandleUpgradeChoicesOffered;
        eventBus.UnitUpgradeSelected -= HandleUpgradeSelected;
        eventBus.UnitRecalled -= HandleUnitRecalled;
        eventBusSubscribed = false;
    }

    private void RegisterWithServiceLocator()
    {
        if (ServiceLocator.TryResolve<PlayerStateController>(out PlayerStateController existingController)
            && existingController != null
            && existingController != this)
        {
            Debug.LogWarning(
                $"{nameof(PlayerStateController)} on '{name}' replaced the previously registered {nameof(PlayerStateController)} on '{existingController.name}'.",
                this);
        }

        ServiceLocator.Register<PlayerStateController>(this);
    }
}
