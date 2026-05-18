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

    [SerializeField, Tooltip("Optional button used to close the upgrade menu without selecting.")]
    private Button closeButton;

    [SerializeField, Tooltip("Optional button used to reroll the active pending offer.")]
    private Button rerollButton;

    [SerializeField, Tooltip("Optional root shown with the reroll control. Defaults to the reroll button object.")]
    private GameObject rerollButtonRoot;

    [SerializeField, Tooltip("Optional TMP text used to display the current reroll cost.")]
    private TMP_Text rerollCostText;

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

        if (upgradesManager != null && upgradesManager.HasPendingOffer(activeUnitId))
        {
            ClearChoices();
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

        if (rerollButtonRoot == null && rerollButton != null)
        {
            rerollButtonRoot = rerollButton.gameObject;
        }
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
            rerollCostText.text = currencyManager == null ? "Free" : rerollCost.ToString();
        }

        GameObject target = rerollButtonRoot != null
            ? rerollButtonRoot
            : rerollButton != null ? rerollButton.gameObject : null;

        if (target != null && target.activeSelf != hasActiveOffer)
        {
            target.SetActive(hasActiveOffer);
        }
    }
}
