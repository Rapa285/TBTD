using UnityEngine;
using UnityEngine.Events;

public class HealthComponent : MonoBehaviour, IDamageable
{
    private float maxHealth;
    private float currentHealth;
    private float currentShield;
    private bool isDead;

    [HideInInspector] public UnityEvent OnDeath;

    private void Start()
    {
        Initialize(100f);
    }

    public void Initialize(float health, float shield = 0f)
    {
        maxHealth = Mathf.Max(1f, health);
        currentHealth = maxHealth;
        currentShield = Mathf.Max(0f, shield);
        isDead = false;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        if (currentShield > 0)
        {
            float absorbed = Mathf.Min(amount, currentShield);
            currentShield -= absorbed;
            amount -= absorbed;
        }

        if (amount > 0)
        {
            currentHealth -= amount;
            Debug.Log($"{gameObject.name} took {amount} damage. Remaining Health: {currentHealth}, Shield: {currentShield}");
        }

        if (currentHealth <= 0f)
        {
            Die();
            Debug.Log($"{gameObject.name} has died.");
        }
    }

    private void Die()
    {
        isDead = true;
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
}