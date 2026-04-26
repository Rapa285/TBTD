using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI recall behavior for one managed unit item.
/// </summary>
[RequireComponent(typeof(UIUnitItem))]
public class UnitUIRecall : MonoBehaviour
{
    [SerializeField, Tooltip("Sibling unit item model used to resolve roster identity and deployment state.")]
    private UIUnitItem uiUnitItem;

    [SerializeField, Tooltip("Sibling deployment behavior used to read hover state.")]
    private UnitUIDeployment unitUiDeployment;

    [SerializeField, Tooltip("Recall button shown when a managed unit is deployed and this item is hovered.")]
    private Button recallButton;

    [SerializeField, Tooltip("Optional root shown and hidden with recall visibility. Defaults to the recall button object.")]
    private GameObject recallButtonRoot;

    private UnitEventBus eventBus;
    private bool subscribedToButton;
    private bool subscribedToEventBus;
    private bool wasHovered;
    private bool wasDeployed;

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
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
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
    }

    private void HandleRecallClicked()
    {
        if (uiUnitItem == null || !uiUnitItem.IsManagedUnitConfigured)
        {
            RefreshState();
            return;
        }

        uiUnitItem.TryRecall();
        RefreshState();
    }

    private void HandleUnitDeployed(UnitDeployedEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            RefreshState();
        }
    }

    private void HandleUnitRecalled(UnitRecalledEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
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

        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }
    }

    private void Subscribe()
    {
        SubscribeToButton();
        SubscribeToEventBus();
    }

    private void Unsubscribe()
    {
        UnsubscribeFromButton();
        UnsubscribeFromEventBus();
    }

    private void SubscribeToButton()
    {
        if (subscribedToButton || recallButton == null)
        {
            return;
        }

        recallButton.onClick.AddListener(HandleRecallClicked);
        subscribedToButton = true;
    }

    private void UnsubscribeFromButton()
    {
        if (!subscribedToButton || recallButton == null)
        {
            return;
        }

        recallButton.onClick.RemoveListener(HandleRecallClicked);
        subscribedToButton = false;
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

        SetRecallVisible(uiUnitItem != null
            && uiUnitItem.IsManagedUnitConfigured
            && isDeployed
            && isHovered);
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

        if (target != null && target.activeSelf != isVisible)
        {
            target.SetActive(isVisible);
        }
    }
}
