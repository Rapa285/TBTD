using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Selection-driven panel for roster/runtime unit identity, XP, ammo, upgrades, and evolution state.
/// </summary>
public class UnitDetailsUI : MonoBehaviour
{
    [SerializeField, Tooltip("Panel root shown while a unit is selected. Defaults to this object.")]
    private GameObject root;

    [SerializeField, Tooltip("Player state source used for selected tower identity.")]
    private PlayerStateController playerStateController;

    [SerializeField, Tooltip("Roster state source used for managed unit metadata, XP, and upgrades.")]
    private UnitStateManager unitStateManager;

    [SerializeField, Tooltip("Event bus used to refresh XP, ammo, upgrade, and recall state for the selected unit.")]
    private UnitEventBus eventBus;

    [SerializeField, Tooltip("Optional visual effect used to animate this panel opening and closing.")]
    private UnitDetailUIFX detailFx;

    [SerializeField, Tooltip("TMP text used to display the selected unit name.")]
    private TMP_Text nameText;

    [SerializeField, Tooltip("Image used to display the selected roster unit icon.")]
    private Image iconImage;

    [SerializeField, Tooltip("TMP text used to display selected unit XP.")]
    private TMP_Text xpText;

    [SerializeField, Tooltip("Slider used to display selected unit XP progress to the next threshold.")]
    private Slider xpSlider;

    [SerializeField, Tooltip("TMP text used to display selected tower ammo units.")]
    private TMP_Text ammoText;

    [SerializeField, Tooltip("Presenter for selected multi-upgrade lines only. Evolution is controlled by the dedicated fields below.")]
    private UnitUpgradeListUI upgradeList;

    [SerializeField, Tooltip("Dedicated evolution slot. UnitDetailsUI controls this separately from the multi-upgrade list.")]
    private UpgradeIconLevelUI evolutionSlot;

    [SerializeField, Tooltip("Convertible hover source for the evolution slot's selected-evolution or no-evolution tooltip.")]
    private ConvertibleUpgradeHoverable evolutionHoverable;

    [SerializeField, Tooltip("Label shown in the evolution slot before the selected unit has evolved.")]
    private string evolutionPlaceholderLabel = "EVO";

    private TowerEntity selectedTower;
    private string selectedUnitId;
    private bool playerStateSubscribed;
    private bool eventBusSubscribed;
    private bool changingRootVisibility;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        Subscribe();
        RefreshFromCurrentSelection();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
        RefreshFromCurrentSelection();
    }

    private void OnDisable()
    {
        if (changingRootVisibility && IsRootSelf())
        {
            return;
        }

        Unsubscribe();
        ClearCachedSelection();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void HandleSelectionChanged(PlayerSelectionChangedEvent eventData)
    {
        RefreshFromSelection(eventData.CurrentTower, eventData.CurrentUnitId);
    }

    private void HandleUnitExperienceChanged(UnitExperienceChangedEvent eventData)
    {
        if (IsSelectedUnit(eventData.UnitId))
        {
            RefreshSelectedXpDisplay();
        }
    }

    private void HandleUnitAmmoConsumed(UnitAmmoConsumedEvent eventData)
    {
        if (IsSelectedTower(eventData.UnitId, eventData.Tower))
        {
            RefreshAmmoDisplay();
        }
    }

    private void HandleTowerModified(TowerModifiedEvent eventData)
    {
        if (IsSelectedTower(eventData.UnitId, eventData.Tower))
        {
            RefreshAmmoDisplay();
        }
    }

    private void HandleUnitUpgradeSelected(UnitUpgradeSelectedEvent eventData)
    {
        if (IsSelectedUnit(eventData.UnitId))
        {
            RefreshSelectedProgressionDisplay();
        }
    }

    private void HandleUnitDeployed(UnitDeployedEvent eventData)
    {
        if (IsSelectedUnit(eventData.UnitId))
        {
            RefreshFromSelection(eventData.Tower, eventData.UnitId);
        }
    }

    private void HandleUnitRecalled(UnitRecalledEvent eventData)
    {
        if (IsSelectedUnit(eventData.UnitId))
        {
            RefreshFromSelection(null, eventData.UnitId);
        }
    }

    private void RefreshFromCurrentSelection()
    {
        ResolveReferences();

        TowerEntity tower = playerStateController != null
            ? playerStateController.SelectedTower
            : null;

        string unitId = playerStateController != null
            ? playerStateController.SelectedUnitId
            : null;

        RefreshFromSelection(tower, unitId);
    }

    private void RefreshFromSelection(TowerEntity tower, string unitId)
    {
        ResolveReferences();

        selectedTower = tower != null && tower.Deployed ? tower : null;
        selectedUnitId = !string.IsNullOrWhiteSpace(unitId)
            ? unitId
            : selectedTower != null ? selectedTower.UnitId : null;

        if (TryGetManagedUnit(out UnitStateManager.OwnedUnitState unit))
        {
            RefreshManagedDisplay(unit);
            SetRootVisible(true);
            return;
        }

        if (selectedTower != null)
        {
            RefreshUnmanagedDisplay(selectedTower);
            SetRootVisible(true);
            return;
        }

        ClearDisplay();
    }

    private void RefreshManagedDisplay(UnitStateManager.OwnedUnitState unit)
    {
        string displayName = !string.IsNullOrWhiteSpace(unit.DisplayName)
            ? unit.DisplayName
            : selectedTower != null ? selectedTower.name : unit.UnitId;

        SetText(nameText, displayName, true);
        SetIcon(unit.Icon);
        RefreshXpDisplay(unit);
        RefreshAmmoDisplay();

        if (upgradeList != null)
        {
            upgradeList.Bind(unit);
        }

        RefreshEvolutionDisplay(unit);
    }

    private void RefreshUnmanagedDisplay(TowerEntity tower)
    {
        SetText(nameText, tower != null ? tower.name : string.Empty, true);
        SetIcon(null);
        ClearXpDisplay();
        RefreshAmmoDisplay();

        if (upgradeList != null)
        {
            upgradeList.Clear();
        }

        ClearEvolutionDisplay();
    }

    private void RefreshXpDisplay(UnitStateManager.OwnedUnitState unit)
    {
        if (unit == null)
        {
            ClearXpDisplay();
            return;
        }

        float currentXp = unit.Experience;
        float nextThreshold = 0f;
        bool hasThreshold = unitStateManager != null
            && unitStateManager.TryGetNextExperienceThreshold(unit.UnitId, out nextThreshold);

        if (hasThreshold)
        {
            SetText(xpText, $"{FormatNumber(currentXp)} / {FormatNumber(nextThreshold)}", true);
            SetSliderValue(nextThreshold > 0f ? Mathf.Clamp01(currentXp / nextThreshold) : 1f, true);
            return;
        }

        SetText(xpText, FormatNumber(currentXp), true);
        SetSliderValue(1f, true);
    }

    private void RefreshAmmoDisplay()
    {
        if (selectedTower == null)
        {
            SetText(ammoText, string.Empty, false);
            return;
        }

        SetText(ammoText, $"{selectedTower.CurrentAmmoUnits} / {selectedTower.MaxAmmoUnits}", true);
    }

    private void RefreshSelectedXpDisplay()
    {
        if (TryGetManagedUnit(out UnitStateManager.OwnedUnitState unit))
        {
            RefreshXpDisplay(unit);
        }
        else
        {
            ClearXpDisplay();
        }
    }

    private void RefreshSelectedProgressionDisplay()
    {
        if (!TryGetManagedUnit(out UnitStateManager.OwnedUnitState unit))
        {
            ClearXpDisplay();
            if (upgradeList != null)
            {
                upgradeList.Clear();
            }

            ClearEvolutionDisplay();
            RefreshAmmoDisplay();
            return;
        }

        RefreshXpDisplay(unit);
        RefreshAmmoDisplay();

        if (upgradeList != null)
        {
            upgradeList.Bind(unit);
        }

        RefreshEvolutionDisplay(unit);
    }

    private void ClearDisplay()
    {
        ClearCachedSelection();
        SetText(nameText, string.Empty, false);
        SetIcon(null);
        ClearXpDisplay();
        SetText(ammoText, string.Empty, false);

        if (upgradeList != null)
        {
            upgradeList.Clear();
        }

        ClearEvolutionDisplay();

        SetRootVisible(false);
    }

    private void ClearXpDisplay()
    {
        SetText(xpText, string.Empty, false);
        SetSliderValue(0f, false);
    }

    private void ClearCachedSelection()
    {
        selectedTower = null;
        selectedUnitId = null;
    }

    private void RefreshEvolutionDisplay(UnitStateManager.OwnedUnitState unit)
    {
        if (unit == null)
        {
            ClearEvolutionDisplay();
            return;
        }

        if (unit.SelectedEvolution != null)
        {
            if (evolutionSlot != null)
            {
                evolutionSlot.BindEvolution(unit.SelectedEvolution);
            }

            if (evolutionHoverable != null)
            {
                evolutionHoverable.Bind(unit.SelectedEvolution);
            }

            return;
        }

        // Keep the evolution slot visible for roster units so its placeholder hover explains the empty state.
        if (evolutionSlot != null)
        {
            evolutionSlot.BindPlaceholder(evolutionPlaceholderLabel);
        }

        if (evolutionHoverable != null)
        {
            evolutionHoverable.BindDefault();
        }
    }

    private void ClearEvolutionDisplay()
    {
        if (evolutionSlot != null)
        {
            evolutionSlot.Clear();
        }

        if (evolutionHoverable != null)
        {
            evolutionHoverable.Clear();
        }
    }

    private bool TryGetManagedUnit(out UnitStateManager.OwnedUnitState unit)
    {
        unit = null;

        if (string.IsNullOrWhiteSpace(selectedUnitId))
        {
            return false;
        }

        if (unitStateManager == null)
        {
            ServiceLocator.TryResolve(out unitStateManager);
        }

        return unitStateManager != null
            && unitStateManager.TryGetUnit(selectedUnitId, out unit);
    }

    private bool IsSelectedUnit(string unitId)
    {
        return !string.IsNullOrWhiteSpace(selectedUnitId)
            && !string.IsNullOrWhiteSpace(unitId)
            && selectedUnitId == unitId;
    }

    private bool IsSelectedTower(string unitId, TowerEntity tower)
    {
        if (selectedTower == null)
        {
            return false;
        }

        return tower == selectedTower || IsSelectedUnit(unitId);
    }

    private void ResolveReferences()
    {
        if (root == null)
        {
            root = gameObject;
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

        if (detailFx == null && root != null)
        {
            detailFx = root.GetComponent<UnitDetailUIFX>();
        }

        if (detailFx == null)
        {
            detailFx = GetComponent<UnitDetailUIFX>();
        }

        if (upgradeList == null)
        {
            upgradeList = GetComponentInChildren<UnitUpgradeListUI>(true);
        }

        if (evolutionHoverable == null && evolutionSlot != null)
        {
            evolutionHoverable = evolutionSlot.GetComponent<ConvertibleUpgradeHoverable>();
        }

        if (evolutionHoverable == null)
        {
            evolutionHoverable = GetComponentInChildren<ConvertibleUpgradeHoverable>(true);
        }
    }

    private void Subscribe()
    {
        SubscribeToPlayerState();
        SubscribeToEventBus();
    }

    private void Unsubscribe()
    {
        UnsubscribeFromPlayerState();
        UnsubscribeFromEventBus();
    }

    private void SubscribeToPlayerState()
    {
        if (playerStateSubscribed)
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
        playerStateSubscribed = true;
    }

    private void UnsubscribeFromPlayerState()
    {
        if (!playerStateSubscribed || playerStateController == null)
        {
            return;
        }

        playerStateController.SelectionChanged -= HandleSelectionChanged;
        playerStateSubscribed = false;
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

        eventBus.UnitExperienceChanged += HandleUnitExperienceChanged;
        eventBus.UnitAmmoConsumed += HandleUnitAmmoConsumed;
        eventBus.TowerModified += HandleTowerModified;
        eventBus.UnitUpgradeSelected += HandleUnitUpgradeSelected;
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

        eventBus.UnitExperienceChanged -= HandleUnitExperienceChanged;
        eventBus.UnitAmmoConsumed -= HandleUnitAmmoConsumed;
        eventBus.TowerModified -= HandleTowerModified;
        eventBus.UnitUpgradeSelected -= HandleUnitUpgradeSelected;
        eventBus.UnitDeployed -= HandleUnitDeployed;
        eventBus.UnitRecalled -= HandleUnitRecalled;
        eventBusSubscribed = false;
    }

    private void SetIcon(Sprite sprite)
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.sprite = sprite;
        iconImage.enabled = sprite != null;
    }

    private static void SetText(TMP_Text text, string value, bool isVisible)
    {
        if (text == null)
        {
            return;
        }

        text.text = value ?? string.Empty;
        text.enabled = isVisible && !string.IsNullOrWhiteSpace(text.text);
    }

    private void SetSliderValue(float value, bool isVisible)
    {
        if (xpSlider == null)
        {
            return;
        }

        xpSlider.minValue = 0f;
        xpSlider.maxValue = 1f;
        xpSlider.value = Mathf.Clamp01(value);

        if (xpSlider.gameObject.activeSelf != isVisible)
        {
            xpSlider.gameObject.SetActive(isVisible);
        }
    }

    private void SetRootVisible(bool isVisible)
    {
        GameObject target = root != null ? root : gameObject;
        if (target == null)
        {
            return;
        }

        if (detailFx != null)
        {
            if (isVisible)
            {
                changingRootVisibility = false;
                detailFx.Show();
            }
            else if (target.activeSelf)
            {
                bool isFxSelfHide = target == gameObject;
                changingRootVisibility = isFxSelfHide;
                detailFx.Hide(() => changingRootVisibility = false);
            }

            return;
        }

        if (target.activeSelf == isVisible)
        {
            return;
        }

        bool isSelfHide = target == gameObject && !isVisible;
        changingRootVisibility = isSelfHide;
        target.SetActive(isVisible);
        changingRootVisibility = false;
    }

    private bool IsRootSelf()
    {
        return (root != null ? root : gameObject) == gameObject;
    }

    private static string FormatNumber(float value)
    {
        return Mathf.Approximately(value, Mathf.Round(value))
            ? Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
