using TMPro;
using UnityEngine;

/// <summary>
/// Displays the current player currency balance.
/// </summary>
public class UICurrencyDisplayer : MonoBehaviour
{
    [SerializeField, Tooltip("TMP text used to display the current currency balance.")]
    private TMP_Text currencyText;

    [SerializeField, Tooltip("Optional prefix shown before the numeric balance.")]
    private string prefix;

    private UnitEventBus eventBus;
    private CurrencyManager currencyManager;
    private bool eventBusSubscribed;

    private void Awake()
    {
        ResolveReferences();
        RefreshDisplay();
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

    private void OnDisable()
    {
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

    private void HandleCurrencyChanged(CurrencyChangedEvent eventData)
    {
        RefreshDisplay(eventData.CurrentCurrency);
    }

    private void ResolveReferences()
    {
        if (currencyText == null)
        {
            currencyText = GetComponent<TMP_Text>();
        }

        if (currencyText == null)
        {
            currencyText = GetComponentInChildren<TMP_Text>(true);
        }

        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }

        if (currencyManager == null)
        {
            ServiceLocator.TryResolve(out currencyManager);
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

        eventBus.CurrencyChanged += HandleCurrencyChanged;
        eventBusSubscribed = true;
    }

    private void UnsubscribeFromEventBus()
    {
        if (!eventBusSubscribed || eventBus == null)
        {
            return;
        }

        eventBus.CurrencyChanged -= HandleCurrencyChanged;
        eventBusSubscribed = false;
    }

    private void RefreshDisplay()
    {
        ResolveReferences();
        RefreshDisplay(currencyManager != null ? currencyManager.CurrentCurrency : 0);
    }

    private void RefreshDisplay(int currency)
    {
        if (currencyText != null)
        {
            currencyText.text = $"{prefix}{Mathf.Max(0, currency)}";
        }
    }
}
