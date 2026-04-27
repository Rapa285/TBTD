using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UIUnitItem))]
public class UnitUICooldownTimer : MonoBehaviour
{
    [SerializeField, Tooltip("Sibling unit item model used to resolve roster identity and cooldown state.")]
    private UIUnitItem uiUnitItem;

    [SerializeField, Tooltip("Image overlay whose fill amount displays remaining cooldown.")]
    private Image cooldownOverlay;

    [SerializeField, Tooltip("TMP text used to show cooldown remaining in seconds.")]
    private TMP_Text cooldownText;

    [SerializeField, Tooltip("Optional root shown only while this unit is cooling down. Defaults to the overlay image object.")]
    private GameObject overlayRoot;

    private UnitEventBus eventBus;
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

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!eventBusSubscribed)
        {
            SubscribeToEventBus();
        }

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

    private void HandleUnitRecalled(UnitRecalledEvent eventData)
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

    private void HandleUnitCooldownEnded(UnitCooldownEndedEvent eventData)
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

        if (overlayRoot == null && cooldownOverlay != null)
        {
            overlayRoot = cooldownOverlay.gameObject;
        }

        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
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

        eventBus.UnitRecalled += HandleUnitRecalled;
        eventBus.UnitDeployed += HandleUnitDeployed;
        eventBus.UnitCooldownEnded += HandleUnitCooldownEnded;
        eventBusSubscribed = true;
    }

    private void UnsubscribeFromEventBus()
    {
        if (!eventBusSubscribed || eventBus == null)
        {
            return;
        }

        eventBus.UnitRecalled -= HandleUnitRecalled;
        eventBus.UnitDeployed -= HandleUnitDeployed;
        eventBus.UnitCooldownEnded -= HandleUnitCooldownEnded;
        eventBusSubscribed = false;
    }

    private void RefreshDisplay()
    {
        ResolveReferences();

        if (!TryGetTimerDisplay(out float remaining, out float normalizedRemaining))
        {
            ClearDisplay();
            return;
        }

        SetOverlayVisible(true);

        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = normalizedRemaining;
            cooldownOverlay.enabled = true;
        }

        if (cooldownText != null)
        {
            cooldownText.text = $"{remaining.ToString("0.0", CultureInfo.InvariantCulture)}s";
            cooldownText.enabled = true;
        }
    }

    private bool TryGetTimerDisplay(out float remaining, out float normalizedRemaining)
    {
        remaining = 0f;
        normalizedRemaining = 0f;

        if (!TryGetManagedUnit(out UnitStateManager.OwnedUnitState unit))
        {
            return false;
        }

        // This overlay intentionally visualizes both unavailable states for the UI slot:
        // recalled-unit DeploymentCooldown first, then deployed-tower SetupTime while combat is arming.
        if (unit.IsCoolingDown)
        {
            remaining = unit.CooldownRemaining;
            normalizedRemaining = unit.CooldownNormalizedRemaining;
            return true;
        }

        TowerEntity runtimeTower = unit.CurrentRuntimeInstance;
        if (runtimeTower == null || !runtimeTower.IsInSetupTime)
        {
            return false;
        }

        remaining = runtimeTower.SetupTimeRemaining;
        normalizedRemaining = runtimeTower.SetupTimeNormalizedRemaining;
        return true;
    }

    private bool TryGetManagedUnit(out UnitStateManager.OwnedUnitState unit)
    {
        if (uiUnitItem == null || !uiUnitItem.IsManagedUnitConfigured)
        {
            unit = null;
            return false;
        }

        return uiUnitItem.TryGetOwnedUnit(out unit);
    }

    private bool IsMatchingUnit(string unitId)
    {
        return uiUnitItem != null
            && uiUnitItem.IsManagedUnit
            && uiUnitItem.UnitId == unitId;
    }

    private void ClearDisplay()
    {
        SetOverlayVisible(false);

        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = 0f;
            cooldownOverlay.enabled = false;
        }

        if (cooldownText != null)
        {
            cooldownText.text = string.Empty;
            cooldownText.enabled = false;
        }
    }

    private void SetOverlayVisible(bool isVisible)
    {
        GameObject target = overlayRoot != null
            ? overlayRoot
            : cooldownOverlay != null ? cooldownOverlay.gameObject : null;

        if (target != null && target != gameObject && target.activeSelf != isVisible)
        {
            target.SetActive(isVisible);
        }
    }
}
