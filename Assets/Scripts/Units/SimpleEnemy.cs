using UnityEngine;

/// <summary>
/// Minimal damageable enemy used to test tower damage and XP rewards.
/// </summary>
public sealed class SimpleEnemy : MonoBehaviour, IAttackContextDamageable, IDamageable
{
    [SerializeField, Min(0.01f), Tooltip("Maximum health restored whenever this enemy is enabled.")]
    private float maxHealth = 5f;

    [SerializeField, Min(0f), Tooltip("XP awarded to the attacking unit when this enemy dies.")]
    private float experienceReward = 1f;

    [SerializeField, Tooltip("Destroy this GameObject on death instead of disabling it.")]
    private bool destroyOnDeath = true;

    private float currentHealth;
    private bool dead;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float ExperienceReward => experienceReward;
    public bool Dead => dead;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;
        dead = false;
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(0.01f, maxHealth);
        experienceReward = Mathf.Max(0f, experienceReward);
    }

    public void TakeDamage(float amount)
    {
        ApplyDamage(amount, null, null);
    }

    public void TakeDamage(float amount, AttackHitContext context)
    {
        ApplyDamage(amount, context.Attacker, context.AttackerRoot);
    }

    private void ApplyDamage(float amount, TowerEntity attacker, Transform attackerRoot)
    {
        if (dead || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        if (currentHealth > 0f)
        {
            return;
        }

        dead = true;
        AwardExperience(attacker, attackerRoot);
        HandleDeath();
    }

    private void AwardExperience(TowerEntity attacker, Transform attackerRoot)
    {
        if (experienceReward <= 0f)
        {
            return;
        }

        // Context-aware hits provide both tower and root so XP can still be awarded after weapon replacement.
        UnitProgression progression = null;
        if (attacker != null)
        {
            progression = attacker.GetComponentInParent<UnitProgression>();
            if (progression == null)
            {
                progression = attacker.GetComponentInChildren<UnitProgression>(true);
            }
        }

        if (progression == null && attackerRoot != null)
        {
            progression = attackerRoot.GetComponentInParent<UnitProgression>();
            if (progression == null)
            {
                progression = attackerRoot.GetComponentInChildren<UnitProgression>(true);
            }
        }

        if (progression != null)
        {
            progression.AddExperience(experienceReward);
        }
    }

    private void HandleDeath()
    {
        if (destroyOnDeath)
        {
            Destroy(gameObject);
            return;
        }

        gameObject.SetActive(false);
    }
}
