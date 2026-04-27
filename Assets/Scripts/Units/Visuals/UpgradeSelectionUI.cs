using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Shows pending upgrade choices and publishes player selection requests through the unit event bus.
/// </summary>
public class UpgradeSelectionUI : MonoBehaviour
{
    [SerializeField, Tooltip("Event bus used to receive offers and publish selected choice requests.")]
    private UnitEventBus eventBus;

    [SerializeField, Tooltip("Canvas group controlled when showing or hiding upgrade choices.")]
    private CanvasGroup canvasGroup;

    [SerializeField, Tooltip("Parent transform that receives instantiated upgrade choice items.")]
    private Transform choicesRoot;

    [SerializeField, Tooltip("Prefab used for each offered upgrade choice.")]
    private UpgradeChoiceItem choiceItemPrefab;

    private readonly List<UpgradeChoiceItem> spawnedItems = new List<UpgradeChoiceItem>();
    private string activeUnitId;
    private bool eventBusSubscribed;

    private void Awake()
    {
        ResolveReferences();
        Hide();
    }

    private void Start()
    {
        ResolveReferences();
        SubscribeToEventBus();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToEventBus();
    }

    private void OnDisable()
    {
        UnsubscribeFromEventBus();
    }

    private void OnValidate()
    {
        ResolveReferences();
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

        if (choiceItemPrefab == null)
        {
            Debug.LogWarning($"{nameof(UpgradeSelectionUI)} cannot show upgrade offer for unit '{eventData.UnitId}' because no choice item prefab is assigned.", this);
            return;
        }

        if (choicesRoot == null)
        {
            Debug.LogWarning($"{nameof(UpgradeSelectionUI)} cannot show upgrade offer for unit '{eventData.UnitId}' because no choices root is assigned.", this);
            return;
        }

        activeUnitId = eventData.UnitId;
        ClearChoices();

        for (int i = 0; i < eventData.Choices.Length; i++)
        {
            UpgradeChoiceItem item = Instantiate(choiceItemPrefab, choicesRoot);
            item.Bind(eventData.Choices[i], i, this);
            spawnedItems.Add(item);
        }

        Show();
    }

    private void HandleUpgradeSelected(UnitUpgradeSelectedEvent eventData)
    {
        if (string.IsNullOrWhiteSpace(activeUnitId) || eventData.UnitId != activeUnitId)
        {
            return;
        }

        // Only close the panel after the authoritative selected event returns for the active unit.
        activeUnitId = null;
        ClearChoices();
        Hide();
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
    }

    private void ClearChoices()
    {
        for (int i = spawnedItems.Count - 1; i >= 0; i--)
        {
            if (spawnedItems[i] != null)
            {
                Destroy(spawnedItems[i].gameObject);
            }
        }

        spawnedItems.Clear();
    }

    private void ResolveReferences()
    {
        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (choicesRoot == null)
        {
            choicesRoot = transform;
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
        eventBusSubscribed = false;
    }
}
