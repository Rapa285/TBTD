using UnityEngine;

[DefaultExecutionOrder(-950)]
public class CurrencyManager : MonoBehaviour
{
    [SerializeField, Min(0), Tooltip("Player currency available when this manager initializes.")]
    private int startingCurrency = 100;

    private int currentCurrency;
    private UnitEventBus eventBus;

    public int CurrentCurrency => currentCurrency;

    private void Awake()
    {
        currentCurrency = Mathf.Max(0, startingCurrency);
        RegisterWithServiceLocator();
        ResolveEventBus();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<CurrencyManager>(this);
    }

    private void OnValidate()
    {
        startingCurrency = Mathf.Max(0, startingCurrency);
    }

    public bool CanAfford(int amount)
    {
        return currentCurrency >= Mathf.Max(0, amount);
    }

    public bool TrySpend(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (!CanAfford(amount))
        {
            return false;
        }

        if (amount == 0)
        {
            return true;
        }

        SetCurrency(currentCurrency - amount);
        return true;
    }

    public void AddCurrency(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount == 0)
        {
            return;
        }

        SetCurrency(currentCurrency + amount);
    }

    private void SetCurrency(int value)
    {
        int previousCurrency = currentCurrency;
        currentCurrency = Mathf.Max(0, value);
        if (currentCurrency == previousCurrency)
        {
            return;
        }

        ResolveEventBus();
        if (eventBus != null)
        {
            eventBus.RaiseCurrencyChanged(new CurrencyChangedEvent(
                previousCurrency,
                currentCurrency,
                currentCurrency - previousCurrency));
        }
    }

    private void ResolveEventBus()
    {
        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }
    }

    private void RegisterWithServiceLocator()
    {
        if (ServiceLocator.TryResolve<CurrencyManager>(out CurrencyManager existingCurrencyManager)
            && existingCurrencyManager != null
            && existingCurrencyManager != this)
        {
            Debug.LogWarning(
                $"{nameof(CurrencyManager)} on '{name}' replaced the previously registered {nameof(CurrencyManager)} on '{existingCurrencyManager.name}'.",
                this);
        }

        ServiceLocator.Register<CurrencyManager>(this);
    }
}
