using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Debug UI helper for force-applying evolutions and triggering normal upgrade offers on clicked roster units.
/// </summary>
public sealed class UnitUpgradeDebugSuite : MonoBehaviour
{
    private enum DebugMode
    {
        None = 0,
        ForceEvolution = 1,
        LevelUp = 2,
        ResetUpgrades = 3
    }

    [Header("References")]
    [SerializeField, Tooltip("Roster state manager used for debug upgrade operations. Falls back to ServiceLocator.")]
    private UnitStateManager unitStateManager;

    [SerializeField, Tooltip("Upgrade manager used as the fallback evolution source. Falls back to ServiceLocator.")]
    private UpgradesManager upgradesManager;

    [SerializeField, Tooltip("Camera used for debug unit raycasts. Falls back to Camera.main or the first active camera.")]
    private Camera raycastCamera;

    [Header("UI")]
    [SerializeField, Tooltip("Root shown while this debug suite is minimized.")]
    private GameObject minimizedRoot;

    [SerializeField, Tooltip("Root shown while this debug suite is maximized.")]
    private GameObject maximizedRoot;

    [SerializeField, Tooltip("Button that toggles this debug suite between minimized and maximized.")]
    private Button toggleButton;

    [SerializeField, Tooltip("Button that enters normal upgrade threshold trigger mode.")]
    private Button levelUpModeButton;

    [SerializeField, Tooltip("Parent for generated evolution debug buttons.")]
    private Transform evolutionButtonRoot;

    [SerializeField, Tooltip("Button prefab instantiated once per configured evolution.")]
    private Button evolutionButtonPrefab;

    [SerializeField, Tooltip("Label used for the generated reset-upgrades button.")]
    private string resetUpgradesButtonLabel = "Reset Upgrades";

    [SerializeField, Tooltip("Optional status text for the active debug mode or last result.")]
    private TMP_Text statusText;

    [Header("Evolution Source")]
    [SerializeField, Tooltip("Explicit debug evolution list. When empty, UpgradesManager.EvolutionPool is used.")]
    private List<EvolutionSO> evolutions = new List<EvolutionSO>();

    [Header("Raycast")]
    [SerializeField, Tooltip("Layers considered valid tower selection targets. Include TowerUnit here and exclude TowerVision.")]
    private LayerMask selectionLayers = ~0;

    [SerializeField, Tooltip("Layers ignored by debug unit raycasts. Include TowerVision here.")]
    private LayerMask selectionPassThroughLayers;

    [SerializeField, Tooltip("Layers that block debug unit raycasts before a valid tower is hit.")]
    private LayerMask selectionBlockingLayers;

    [SerializeField, Min(0.01f), Tooltip("Maximum distance used by debug unit raycasts.")]
    private float maxSelectionRayDistance = 1000f;

    private readonly List<Button> generatedEvolutionButtons = new List<Button>();
    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
    private PointerEventData pointerEventData;
    private EventSystem pointerEventSystem;
    private DebugMode activeMode;
    private EvolutionSO activeEvolution;
    private bool isMaximized;
    private bool toggleSubscribed;
    private bool levelUpSubscribed;

    private void Awake()
    {
        ResolveReferences();
        RebuildEvolutionButtons();
        SetMaximized(false);
    }

    private void Start()
    {
        ResolveReferences();
        SetMaximized(isMaximized);
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeButtons();
        SetMaximized(isMaximized);
    }

    private void OnDisable()
    {
        CancelMode();
        UnsubscribeButtons();
    }

    private void OnValidate()
    {
        maxSelectionRayDistance = Mathf.Max(0.01f, maxSelectionRayDistance);
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || activeMode == DebugMode.None)
        {
            return;
        }

        if (mouse.rightButton.wasPressedThisFrame)
        {
            CancelMode();
            return;
        }

        if (!mouse.leftButton.wasPressedThisFrame)
        {
            return;
        }

        Vector2 screenPosition = mouse.position.ReadValue();
        if (IsPointerOverBlockingUI(screenPosition))
        {
            return;
        }

        if (!TryResolveRosterUnit(screenPosition, out string unitId))
        {
            SetStatus("No valid roster unit hit.");
            return;
        }

        ApplyActiveMode(unitId);
    }

    public void ToggleMaximized()
    {
        SetMaximized(!isMaximized);
    }

    public void EnterLevelUpMode()
    {
        activeMode = DebugMode.LevelUp;
        activeEvolution = null;
        SetStatus("Insta Lvl Up: click a deployed roster unit. Right click cancels.");
    }

    public void EnterForceEvolutionMode(EvolutionSO evolution)
    {
        if (evolution == null || !evolution.HasResolvedUpgrade)
        {
            SetStatus("Invalid evolution.");
            return;
        }

        activeMode = DebugMode.ForceEvolution;
        activeEvolution = evolution;
        SetStatus($"EVO {GetEvolutionLabel(evolution)}: click a deployed roster unit. Right click cancels.");
    }

    public void EnterResetUpgradesMode()
    {
        activeMode = DebugMode.ResetUpgrades;
        activeEvolution = null;
        SetStatus("Reset Upgrades: click a deployed roster unit. Right click cancels.");
    }

    public void CancelMode()
    {
        activeMode = DebugMode.None;
        activeEvolution = null;
        SetStatus(string.Empty);
    }

    public void RebuildEvolutionButtons()
    {
        ClearGeneratedEvolutionButtons();

        if (evolutionButtonRoot == null || evolutionButtonPrefab == null)
        {
            return;
        }

        Button resetButton = Instantiate(evolutionButtonPrefab, evolutionButtonRoot);
        resetButton.onClick.AddListener(EnterResetUpgradesMode);
        SetButtonLabel(resetButton, resetUpgradesButtonLabel);
        resetButton.gameObject.SetActive(true);
        generatedEvolutionButtons.Add(resetButton);

        IReadOnlyList<EvolutionSO> source = GetEvolutionSource();
        for (int i = 0; i < source.Count; i++)
        {
            EvolutionSO evolution = source[i];
            if (evolution == null)
            {
                continue;
            }

            Button button = Instantiate(evolutionButtonPrefab, evolutionButtonRoot);
            EvolutionSO capturedEvolution = evolution;
            button.onClick.AddListener(() => EnterForceEvolutionMode(capturedEvolution));
            SetButtonLabel(button, GetEvolutionLabel(evolution));
            button.gameObject.SetActive(true);
            generatedEvolutionButtons.Add(button);
        }
    }

    private void ApplyActiveMode(string unitId)
    {
        ResolveReferences();
        if (unitStateManager == null)
        {
            SetStatus("Missing UnitStateManager.");
            return;
        }

        switch (activeMode)
        {
            case DebugMode.ForceEvolution:
                if (unitStateManager.DebugForceEvolution(unitId, activeEvolution))
                {
                    SetStatus($"Applied EVO {GetEvolutionLabel(activeEvolution)} to {unitId}.");
                }
                else
                {
                    SetStatus($"Could not apply EVO to {unitId}.");
                }

                break;

            case DebugMode.LevelUp:
                if (unitStateManager.DebugTriggerUpgradeThreshold(unitId))
                {
                    SetStatus($"Triggered upgrade offer for {unitId}.");
                }
                else
                {
                    SetStatus($"Could not trigger upgrade offer for {unitId}.");
                }

                break;

            case DebugMode.ResetUpgrades:
                if (unitStateManager.DebugResetUpgrades(unitId))
                {
                    SetStatus($"Reset upgrades for {unitId}.");
                }
                else
                {
                    SetStatus($"No upgrades reset for {unitId}.");
                }

                break;
        }
    }

    private IReadOnlyList<EvolutionSO> GetEvolutionSource()
    {
        if (evolutions != null && evolutions.Count > 0)
        {
            return evolutions;
        }

        ResolveReferences();
        return upgradesManager != null
            ? upgradesManager.EvolutionPool
            : Array.Empty<EvolutionSO>();
    }

    private bool TryResolveRosterUnit(Vector2 screenPosition, out string unitId)
    {
        unitId = null;

        TowerSelectionTarget selectionTarget = GetSelectionTarget(screenPosition);
        if (selectionTarget == null
            || !selectionTarget.TryGetSelectableTower(out TowerEntity tower)
            || tower == null
            || string.IsNullOrWhiteSpace(tower.UnitId)
            || unitStateManager == null
            || !unitStateManager.TryGetUnit(tower.UnitId, out UnitStateManager.OwnedUnitState unit)
            || unit.CurrentRuntimeInstance != tower)
        {
            return false;
        }

        unitId = tower.UnitId;
        return true;
    }

    private TowerSelectionTarget GetSelectionTarget(Vector2 screenPosition)
    {
        Camera cameraToUse = ResolveRaycastCamera();
        if (cameraToUse == null)
        {
            return null;
        }

        int raycastLayerMask = selectionLayers.value | selectionPassThroughLayers.value | selectionBlockingLayers.value;
        if (raycastLayerMask == 0)
        {
            return null;
        }

        Ray ray = cameraToUse.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            maxSelectionRayDistance,
            raycastLayerMask,
            QueryTriggerInteraction.Collide);

        if (hits.Length == 0)
        {
            return null;
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
                    return target;
                }

                return null;
            }

            if (IsInLayer(hitObject, selectionBlockingLayers))
            {
                return null;
            }
        }

        return null;
    }

    private bool IsPointerOverBlockingUI(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }

        if (pointerEventData == null || pointerEventSystem != eventSystem)
        {
            pointerEventData = new PointerEventData(eventSystem);
            pointerEventSystem = eventSystem;
        }

        pointerEventData.Reset();
        pointerEventData.position = screenPosition;
        pointerEventData.button = PointerEventData.InputButton.Left;
        uiRaycastResults.Clear();
        eventSystem.RaycastAll(pointerEventData, uiRaycastResults);

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

    private void SetMaximized(bool value)
    {
        isMaximized = value;

        if (minimizedRoot != null && minimizedRoot.activeSelf != !isMaximized)
        {
            minimizedRoot.SetActive(!isMaximized);
        }

        if (maximizedRoot != null && maximizedRoot.activeSelf != isMaximized)
        {
            maximizedRoot.SetActive(isMaximized);
        }

        if (!isMaximized)
        {
            CancelMode();
        }
    }

    private void ResolveReferences()
    {
        if (unitStateManager == null)
        {
            ServiceLocator.TryResolve(out unitStateManager);
        }

        if (upgradesManager == null)
        {
            ServiceLocator.TryResolve(out upgradesManager);
        }

        if (toggleButton == null)
        {
            toggleButton = GetComponentInChildren<Button>(true);
        }

        ResolveRaycastCamera();
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

    private void SubscribeButtons()
    {
        if (!toggleSubscribed && toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleMaximized);
            toggleSubscribed = true;
        }

        if (!levelUpSubscribed && levelUpModeButton != null)
        {
            levelUpModeButton.onClick.AddListener(EnterLevelUpMode);
            levelUpSubscribed = true;
        }
    }

    private void UnsubscribeButtons()
    {
        if (toggleSubscribed && toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(ToggleMaximized);
            toggleSubscribed = false;
        }

        if (levelUpSubscribed && levelUpModeButton != null)
        {
            levelUpModeButton.onClick.RemoveListener(EnterLevelUpMode);
            levelUpSubscribed = false;
        }
    }

    private void ClearGeneratedEvolutionButtons()
    {
        for (int i = 0; i < generatedEvolutionButtons.Count; i++)
        {
            Button button = generatedEvolutionButtons[i];
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                Destroy(button.gameObject);
            }
        }

        generatedEvolutionButtons.Clear();
    }

    private void SetStatus(string value)
    {
        if (statusText != null)
        {
            statusText.text = value ?? string.Empty;
        }
    }

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = label;
        }
    }

    private static string GetEvolutionLabel(EvolutionSO evolution)
    {
        if (evolution == null)
        {
            return "Invalid EVO";
        }

        UpgradeSO resolvedUpgrade = evolution.ResolvedUpgrade;
        if (resolvedUpgrade != null && !string.IsNullOrWhiteSpace(resolvedUpgrade.UpgradeName))
        {
            return resolvedUpgrade.UpgradeName;
        }

        return evolution.name;
    }

    private static int CompareRaycastHitDistance(RaycastHit left, RaycastHit right)
    {
        return left.distance.CompareTo(right.distance);
    }

    private static bool IsInLayer(GameObject target, LayerMask layerMask)
    {
        return target != null && (layerMask.value & (1 << target.layer)) != 0;
    }
}
