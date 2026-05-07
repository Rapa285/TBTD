using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class GiveCurrencyOnDeath : MonoBehaviour
{
    [SerializeField, Min(0), Tooltip("Currency granted when this health component dies.")]
    private int currencyReward = 1;

    [SerializeField, Tooltip("Log a warning if no CurrencyManager is registered when this entity dies.")]
    private bool warnIfCurrencyManagerMissing = true;

    private HealthComponent health;
    private bool warnedMissingCurrencyManager;

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
        if (health != null)
        {
            health.OnDeath.AddListener(GiveCurrency);
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDeath.RemoveListener(GiveCurrency);
        }
    }

    private void OnValidate()
    {
        currencyReward = Mathf.Max(0, currencyReward);
    }

    private void GiveCurrency()
    {
        if (currencyReward <= 0)
        {
            return;
        }

        if (ServiceLocator.TryResolve(out CurrencyManager currencyManager) && currencyManager != null)
        {
            currencyManager.AddCurrency(currencyReward);
            return;
        }

        if (warnIfCurrencyManagerMissing && !warnedMissingCurrencyManager)
        {
            warnedMissingCurrencyManager = true;
            Debug.LogWarning(
                $"{nameof(GiveCurrencyOnDeath)} on '{name}' could not find a {nameof(CurrencyManager)}. No currency was granted.",
                this);
        }
    }
}
