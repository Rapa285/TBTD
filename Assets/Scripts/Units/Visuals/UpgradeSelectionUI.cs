using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Shows pending upgrade choices and publishes player selection requests through the unit event bus.
/// </summary>
public class UpgradeSelectionUI : MonoBehaviour
{
    [SerializeField, Tooltip("Event bus used to receive offers and publish selected choice requests.")]
    private UnitEventBus eventBus;

    [SerializeField, Tooltip("Canvas group controlled when showing or hiding upgrade choices.")]
    private CanvasGroup canvasGroup;

    [SerializeField, Tooltip("Parent transform that holds pooled upgrade choice items.")]
    private Transform choicesRoot;

    [SerializeField, Tooltip("Prefab used only when the choices root pool needs to grow.")]
    private UpgradeChoiceItem choiceItemPrefab;

    [SerializeField, Tooltip("Optional details panel updated when an upgrade choice is hovered or focused.")]
    private UpgradeInfoDetailsUI upgradeInfoDetailsUI;

    [SerializeField, Tooltip("Optional strip showing the active unit's already purchased upgrades.")]
    private UpgradeSelectionExistingUpgradesUI existingUpgradesUI;

    [SerializeField, Tooltip("Optional button used to close the upgrade menu without selecting.")]
    private Button closeButton;

    [SerializeField, Tooltip("Optional button used to reroll the active pending offer.")]
    private Button rerollButton;

    [SerializeField, Tooltip("Optional root shown with the reroll control. Defaults to the reroll button object.")]
    private GameObject rerollButtonRoot;

    [SerializeField, Tooltip("Optional TMP text used to display the current reroll cost.")]
    private TMP_Text rerollCostText;

    [SerializeField, Tooltip("Roster manager used to resolve the active offer unit icon and upgrade-credit count.")]
    private UnitStateManager unitStateManager;

    [SerializeField, Tooltip("Optional image used to display the active roster unit icon.")]
    private Image unitIconImage;

    [SerializeField, Tooltip("Optional root shown while the active roster unit has an icon. Leave empty to toggle only the icon Image.")]
    private GameObject unitIconRoot;

    [SerializeField, Tooltip("Optional TMP text used to display the active unit's remaining upgrade credits.")]
    private TMP_Text remainingUpgradesText;

    [SerializeField, Tooltip("Optional root shown while an active roster unit is bound. Defaults to the remaining-upgrades text object.")]
    private GameObject remainingUpgradesRoot;

    [SerializeField, Tooltip("Composite format used to display remaining upgrade credits. {0} is the credit count.")]
    private string remainingUpgradesFormat = "Remaining upgrades : {0}";

    [SerializeField, Min(0f), Tooltip("Delay in seconds between each active upgrade choice reveal animation.")]
    private float choiceRevealDelay = 0.08f;

    private readonly List<UpgradeChoiceItem> pooledItems = new List<UpgradeChoiceItem>();
    private UpgradesManager upgradesManager;
    private CurrencyManager currencyManager;
    private string activeUnitId;
    private bool eventBusSubscribed;
    private bool closeButtonSubscribed;
    private bool rerollButtonSubscribed;

    private void Awake()
    {
        ResolveReferences();
        CollectChoicePool();
        ClearChoices();
        Hide();
    }

    private void Start()
    {
        ResolveReferences();
        CollectChoicePool();
        SubscribeToButtons();
        SubscribeToEventBus();
    }

    private void OnEnable()
    {
        ResolveReferences();
        CollectChoicePool();
        SubscribeToButtons();
        SubscribeToEventBus();
        RefreshRerollState();
    }

    private void OnDisable()
    {
        UnsubscribeFromButtons();
        UnsubscribeFromEventBus();
    }

    private void OnValidate()
    {
        ResolveReferences();
        choiceRevealDelay = Mathf.Max(0f, choiceRevealDelay);
    }

    /// <summary>
    /// Called by child choice items after the player clicks one option in the active pending offer.
    /// </summary>
    public void HandleChoiceSelected(int choiceIndex)
    {
        if (eventBus == null)
        {
            ResolveReferences();
        }

        if (eventBus == null)
        {
            Debug.LogWarning($"{nameof(UpgradeSelectionUI)} cannot request upgrade choice because no {nameof(UnitEventBus)} is assigned.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(activeUnitId))
        {
            Debug.LogWarning($"{nameof(UpgradeSelectionUI)} ignored upgrade choice {choiceIndex} because no active unit offer is stored.", this);
            return;
        }

        eventBus.RaiseUnitUpgradeChoiceRequested(new UnitUpgradeChoiceRequestedEvent(activeUnitId, choiceIndex));
    }

    /// <summary>
    /// Called by child choice items after pointer hover or UI focus changes.
    /// </summary>
    public void HandleChoiceFocused(int choiceIndex)
    {
        if (choiceIndex < 0 || choiceIndex >= pooledItems.Count)
        {
            ClearDetails();
            return;
        }

        UpgradeChoiceItem item = pooledItems[choiceIndex];
        if (item == null)
        {
            ClearDetails();
            return;
        }

        BindDetails(item.Choice);
    }

    /// <summary>
    /// Closes the active upgrade menu without selecting an upgrade.
    /// </summary>
    public void Close()
    {
        string closedUnitId = activeUnitId;
        activeUnitId = null;
        ClearChoices();
        Hide();

        if (eventBus == null)
        {
            ResolveReferences();
        }

        if (eventBus != null)
        {
            eventBus.RaiseUnitUpgradeMenuClosed(new UnitUpgradeMenuClosedEvent(closedUnitId));
        }
    }

    /// <summary>
    /// Requests a currency-backed reroll for the active pending offer.
    /// </summary>
    public void RequestReroll()
    {
        if (eventBus == null)
        {
            ResolveReferences();
        }

        if (eventBus == null || string.IsNullOrWhiteSpace(activeUnitId))
        {
            RefreshRerollState();
            return;
        }

        eventBus.RaiseUnitUpgradeRerollRequested(new UnitUpgradeRerollRequestedEvent(activeUnitId));
        RefreshRerollState();
    }

    private void HandleUpgradeChoicesOffered(UnitUpgradeChoicesOfferedEvent eventData)
    {
        if (string.IsNullOrWhiteSpace(eventData.UnitId))
        {
            Debug.LogWarning($"{nameof(UpgradeSelectionUI)} ignored upgrade offer with a blank unit id.", this);
            return;
        }

        if (eventData.Choices == null || eventData.Choices.Length == 0)
        {
            Debug.LogWarning($"{nameof(UpgradeSelectionUI)} ignored empty upgrade offer for unit '{eventData.UnitId}'.", this);
            return;
        }

        if (choicesRoot == null)
        {
            Debug.LogWarning($"{nameof(UpgradeSelectionUI)} cannot show upgrade offer for unit '{eventData.UnitId}' because no choices root is assigned.", this);
            return;
        }

        if (!EnsureChoiceCapacity(eventData.Choices.Length, eventData.UnitId))
        {
            return;
        }

        activeUnitId = eventData.UnitId;
        ClearChoices();
        RefreshActiveUnitDisplay();
        RefreshExistingUpgrades();

        for (int i = 0; i < eventData.Choices.Length; i++)
        {
            UpgradeChoiceItem item = pooledItems[i];
            item.gameObject.SetActive(true);
            item.Bind(eventData.Choices[i], i, this);
        }

        PlayChainedChoiceReveal();
        BindFirstChoiceDetails();
        Show();
        RefreshRerollState();
    }

    private void HandleUpgradeSelected(UnitUpgradeSelectedEvent eventData)
    {
        if (string.IsNullOrWhiteSpace(activeUnitId) || eventData.UnitId != activeUnitId)
        {
            return;
        }

        if (upgradesManager == null)
        {
            ResolveReferences();
        }

        RefreshActiveUnitDisplay();

        if (upgradesManager != null && upgradesManager.HasPendingOffer(activeUnitId))
        {
            ClearChoices();
            RefreshActiveUnitDisplay();
            RefreshExistingUpgrades();
            RefreshRerollState();
            return;
        }

        // Only close the panel after the authoritative selected event returns for the active unit with no chained offer.
        activeUnitId = null;
        ClearChoices();
        Hide();
    }

    private void HandleCurrencyChanged(CurrencyChangedEvent eventData)
    {
        RefreshRerollState();
    }

    private void HandleUpgradeRerollStateChanged(UpgradeRerollStateChangedEvent eventData)
    {
        RefreshRerollState();
    }

    private void HandleCloseClicked()
    {
        Close();
    }

    private void HandleRerollClicked()
    {
        RequestReroll();
    }

    private void Show()
    {
        SetVisible(true);
    }

    private void Hide()
    {
        ClearActiveUnitDisplay();
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
        RefreshRerollState();
    }

    private void ClearChoices()
    {
        ClearDetails();

        for (int i = 0; i < pooledItems.Count; i++)
        {
            UpgradeChoiceItem item = pooledItems[i];
            if (item == null)
            {
                continue;
            }

            item.ClearBinding();
            item.gameObject.SetActive(false);
        }

        if (existingUpgradesUI != null)
        {
            existingUpgradesUI.Clear();
        }

        ClearActiveUnitDisplay();
    }

    private void BindFirstChoiceDetails()
    {
        for (int i = 0; i < pooledItems.Count; i++)
        {
            if (pooledItems[i] != null && pooledItems[i].gameObject.activeSelf && pooledItems[i].Choice.IsValid)
            {
                BindDetails(pooledItems[i].Choice);
                return;
            }
        }

        ClearDetails();
    }

    private void BindDetails(UnitUpgradeOfferChoice choice)
    {
        if (upgradeInfoDetailsUI == null)
        {
            ResolveReferences();
        }

        if (upgradeInfoDetailsUI != null)
        {
            upgradeInfoDetailsUI.Bind(activeUnitId, choice);
        }
    }

    private void ClearDetails()
    {
        if (upgradeInfoDetailsUI != null)
        {
            upgradeInfoDetailsUI.Clear();
        }
    }

    private void PlayChainedChoiceReveal()
    {
        int revealIndex = 0;

        for (int i = 0; i < pooledItems.Count; i++)
        {
            UpgradeChoiceItem item = pooledItems[i];
            if (!ShouldRevealChoice(item) || !item.TryGetComponent(out UpgradeItemFX itemFx))
            {
                continue;
            }

            itemFx.PrepareRevealAnimation();
        }

        for (int i = 0; i < pooledItems.Count; i++)
        {
            UpgradeChoiceItem item = pooledItems[i];
            if (!ShouldRevealChoice(item) || !item.TryGetComponent(out UpgradeItemFX itemFx))
            {
                continue;
            }

            itemFx.PlayRevealAnimation(revealIndex * choiceRevealDelay);
            revealIndex++;
        }
    }

    private bool ShouldRevealChoice(UpgradeChoiceItem item)
    {
        return item != null
            && item.gameObject.activeSelf
            && item.Choice.IsValid;
    }

    private void ResolveReferences()
    {
        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }

        if (upgradesManager == null)
        {
            ServiceLocator.TryResolve(out upgradesManager);
        }

        if (currencyManager == null)
        {
            ServiceLocator.TryResolve(out currencyManager);
        }

        if (unitStateManager == null)
        {
            ServiceLocator.TryResolve(out unitStateManager);
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (choicesRoot == null)
        {
            choicesRoot = transform;
        }

        if (upgradeInfoDetailsUI == null)
        {
            upgradeInfoDetailsUI = GetComponentInChildren<UpgradeInfoDetailsUI>(true);
        }

        if (existingUpgradesUI == null)
        {
            existingUpgradesUI = GetComponentInChildren<UpgradeSelectionExistingUpgradesUI>(true);
        }

        if (existingUpgradesUI == null)
        {
            existingUpgradesUI = ResolveRuntimeExistingUpgradesUI();
        }

        if (rerollButtonRoot == null && rerollButton != null)
        {
            rerollButtonRoot = rerollButton.gameObject;
        }

        ResolveActiveUnitDisplayReferences();
    }

    private void CollectChoicePool()
    {
        if (choicesRoot == null)
        {
            return;
        }

        UpgradeChoiceItem[] childItems = choicesRoot.GetComponentsInChildren<UpgradeChoiceItem>(true);
        for (int i = 0; i < childItems.Length; i++)
        {
            UpgradeChoiceItem item = childItems[i];
            if (item != null && !pooledItems.Contains(item))
            {
                pooledItems.Add(item);
            }
        }
    }

    private bool EnsureChoiceCapacity(int requiredCount, string unitId)
    {
        CollectChoicePool();

        if (pooledItems.Count >= requiredCount)
        {
            return true;
        }

        if (choiceItemPrefab == null)
        {
            Debug.LogWarning(
                $"{nameof(UpgradeSelectionUI)} cannot show upgrade offer for unit '{unitId}' because the choice pool has {pooledItems.Count} item(s), needs {requiredCount}, and no growth prefab is assigned.",
                this);
            return false;
        }

        while (pooledItems.Count < requiredCount)
        {
            UpgradeChoiceItem item = Instantiate(choiceItemPrefab, choicesRoot);
            item.gameObject.SetActive(false);
            pooledItems.Add(item);
        }

        return true;
    }

    private void SubscribeToButtons()
    {
        if (!closeButtonSubscribed && closeButton != null)
        {
            closeButton.onClick.AddListener(HandleCloseClicked);
            closeButtonSubscribed = true;
        }

        if (!rerollButtonSubscribed && rerollButton != null)
        {
            rerollButton.onClick.AddListener(HandleRerollClicked);
            rerollButtonSubscribed = true;
        }
    }

    private void UnsubscribeFromButtons()
    {
        if (closeButtonSubscribed && closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseClicked);
            closeButtonSubscribed = false;
        }

        if (rerollButtonSubscribed && rerollButton != null)
        {
            rerollButton.onClick.RemoveListener(HandleRerollClicked);
            rerollButtonSubscribed = false;
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

        eventBus.UnitUpgradeChoicesOffered += HandleUpgradeChoicesOffered;
        eventBus.UnitUpgradeSelected += HandleUpgradeSelected;
        eventBus.UpgradeRerollStateChanged += HandleUpgradeRerollStateChanged;
        eventBus.CurrencyChanged += HandleCurrencyChanged;
        eventBusSubscribed = true;
    }

    private void UnsubscribeFromEventBus()
    {
        if (!eventBusSubscribed || eventBus == null)
        {
            return;
        }

        eventBus.UnitUpgradeChoicesOffered -= HandleUpgradeChoicesOffered;
        eventBus.UnitUpgradeSelected -= HandleUpgradeSelected;
        eventBus.UpgradeRerollStateChanged -= HandleUpgradeRerollStateChanged;
        eventBus.CurrencyChanged -= HandleCurrencyChanged;
        eventBusSubscribed = false;
    }

    private void RefreshRerollState()
    {
        if (upgradesManager == null || currencyManager == null)
        {
            ResolveReferences();
        }

        bool hasActiveOffer = !string.IsNullOrWhiteSpace(activeUnitId)
            && upgradesManager != null
            && upgradesManager.HasPendingOffer(activeUnitId);

        bool hasAlternateOffer = hasActiveOffer
            && upgradesManager.CanRerollPendingOffer(activeUnitId);

        int rerollCost = upgradesManager != null ? upgradesManager.CurrentRerollCost : 0;
        bool canAfford = currencyManager == null || currencyManager.CanAfford(rerollCost);
        bool canReroll = hasAlternateOffer && canAfford;

        if (rerollButton != null)
        {
            rerollButton.interactable = canReroll;
        }

        if (rerollCostText != null)
        {
            if (upgradesManager != null && upgradesManager.HasFreeRerolls)
            {
                rerollCostText.text = $"Free x{upgradesManager.FreeRerollsRemaining}";
            }
            else
            {
                rerollCostText.text = currencyManager == null ? "Free" : rerollCost.ToString();
            }
        }

        GameObject target = rerollButtonRoot != null
            ? rerollButtonRoot
            : rerollButton != null ? rerollButton.gameObject : null;

        if (target != null && target.activeSelf != hasActiveOffer)
        {
            target.SetActive(hasActiveOffer);
        }
    }

    private void RefreshExistingUpgrades()
    {
        if (existingUpgradesUI == null)
        {
            ResolveReferences();
        }

        if (existingUpgradesUI == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(activeUnitId))
        {
            existingUpgradesUI.Clear();
            return;
        }

        existingUpgradesUI.Bind(activeUnitId);
    }

    private void RefreshActiveUnitDisplay()
    {
        ResolveReferences();

        if (string.IsNullOrWhiteSpace(activeUnitId)
            || unitStateManager == null
            || !unitStateManager.TryGetUnit(activeUnitId, out UnitStateManager.OwnedUnitState unit))
        {
            ClearActiveUnitDisplay();
            return;
        }

        BindUnitIcon(unit.Icon);
        BindRemainingUpgrades(unit.UnspentUpgradeCount);
    }

    private void BindUnitIcon(Sprite icon)
    {
        ResolveActiveUnitDisplayReferences();

        bool hasIcon = icon != null && unitIconImage != null;
        if (unitIconImage != null)
        {
            unitIconImage.sprite = icon;
            unitIconImage.enabled = hasIcon;
        }

        SetRootVisible(GetUnitIconRootTarget(), hasIcon);
    }

    private void BindRemainingUpgrades(int count)
    {
        ResolveActiveUnitDisplayReferences();

        if (remainingUpgradesText == null)
        {
            SetRootVisible(GetRemainingUpgradesRootTarget(), false);
            return;
        }

        string format = string.IsNullOrWhiteSpace(remainingUpgradesFormat)
            ? "{0}"
            : remainingUpgradesFormat;

        remainingUpgradesText.text = string.Format(format, Mathf.Max(0, count));
        remainingUpgradesText.enabled = true;
        SetRootVisible(GetRemainingUpgradesRootTarget(), true);
    }

    private void ClearActiveUnitDisplay()
    {
        if (unitIconImage != null)
        {
            unitIconImage.sprite = null;
            unitIconImage.enabled = false;
        }

        SetRootVisible(GetUnitIconRootTarget(), false);

        if (remainingUpgradesText != null)
        {
            remainingUpgradesText.text = string.Empty;
            remainingUpgradesText.enabled = false;
        }

        SetRootVisible(GetRemainingUpgradesRootTarget(), false);
    }

    private void ResolveActiveUnitDisplayReferences()
    {
        if (unitIconImage == null)
        {
            Transform characterSprite = FindChildByName(transform, "CharacterSprite");
            if (characterSprite != null)
            {
                unitIconImage = characterSprite.GetComponent<Image>();
            }
        }

        if (remainingUpgradesText == null)
        {
            remainingUpgradesText = FindRemainingUpgradesText(transform);
        }

        if (remainingUpgradesRoot == null && remainingUpgradesText != null)
        {
            remainingUpgradesRoot = remainingUpgradesText.gameObject;
        }
    }

    private GameObject GetUnitIconRootTarget()
    {
        return unitIconRoot;
    }

    private GameObject GetRemainingUpgradesRootTarget()
    {
        return remainingUpgradesRoot != null
            ? remainingUpgradesRoot
            : remainingUpgradesText != null ? remainingUpgradesText.gameObject : null;
    }

    private static void SetRootVisible(GameObject target, bool isVisible)
    {
        if (target != null && target.activeSelf != isVisible)
        {
            target.SetActive(isVisible);
        }
    }

    private UpgradeSelectionExistingUpgradesUI ResolveRuntimeExistingUpgradesUI()
    {
        Transform stripRoot = FindChildByName(transform, "PurchasedUpgrades");
        if (stripRoot == null)
        {
            stripRoot = FindChildByName(transform, "ExistingUpgrades");
        }

        if (stripRoot == null)
        {
            stripRoot = FindChildByName(transform, "Existing Upgrades");
        }

        if (stripRoot == null)
        {
            return null;
        }

        if (!stripRoot.TryGetComponent(out UpgradeSelectionExistingUpgradesUI resolvedExistingUpgradesUI))
        {
            resolvedExistingUpgradesUI = stripRoot.gameObject.AddComponent<UpgradeSelectionExistingUpgradesUI>();
        }

        resolvedExistingUpgradesUI.Configure(stripRoot.gameObject, stripRoot, null, null);
        return resolvedExistingUpgradesUI;
    }

    private static Transform FindChildByName(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform match = FindChildByName(child, childName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static TMP_Text FindRemainingUpgradesText(Transform parent)
    {
        if (parent == null)
        {
            return null;
        }

        TMP_Text[] texts = parent.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text != null && text.text.Contains("Remaining upgrades"))
            {
                return text;
            }
        }

        return null;
    }
}
