using UnityEngine;
using UnityEngine.Events;

public enum HealthDeathMode
{
    DestroyGameObject = 0,
    DisableGameObject = 1,
    None = 2
}

public class HealthComponent : MonoBehaviour, IAttackContextDamageable, IDamageable
{
    [Header("Auto Initialization")]
    [SerializeField] private bool initializeOnStart = false;
    [SerializeField, Min(1f)] private float startingHealth = 100f;
    [SerializeField, Min(0f)] private float startingShield = 0f;

    [Header("Death")]
    [SerializeField] private HealthDeathMode deathMode = HealthDeathMode.DestroyGameObject;

    private float maxHealth;
    private float currentHealth;
    private float currentShield;
    private bool isDead;
    private bool hasLastHitContext;
    private AttackHitContext lastHitContext;

    [HideInInspector] public UnityEvent OnDeath = new UnityEvent();

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float CurrentShield => currentShield;
    public bool IsDead => isDead;
    public bool HasLastHitContext => hasLastHitContext;
    public AttackHitContext LastHitContext => lastHitContext;
    public TowerEntity LastAttacker => hasLastHitContext ? lastHitContext.Attacker : null;
    public Transform LastAttackerRoot => hasLastHitContext ? lastHitContext.AttackerRoot : null;
    public AttackBehaviour LastAttackBehaviour => hasLastHitContext ? lastHitContext.AttackBehaviour : null;
    public BaseProjectile LastProjectile => hasLastHitContext ? lastHitContext.Projectile : null;

    private void Awake()
    {
        EnsureDeathEvent();
    }

    private void Start()
    {
        if (initializeOnStart)
        {
            Initialize(startingHealth, startingShield);
        }
    }

    private void OnValidate()
    {
        startingHealth = Mathf.Max(1f, startingHealth);
        startingShield = Mathf.Max(0f, startingShield);
        EnsureDeathEvent();
    }

    public void Initialize(float health, float shield = 0f)
    {
        maxHealth = Mathf.Max(1f, health);
        currentHealth = maxHealth;
        currentShield = Mathf.Max(0f, shield);
        isDead = false;
        hasLastHitContext = false;
        lastHitContext = default;
        EnsureDeathEvent();
    }

    public void TakeDamage(float amount)
    {
        ApplyDamage(amount, default, false);
    }

    public void TakeDamage(float amount, AttackHitContext context)
    {
        ApplyDamage(amount, context, true);
    }

    private void ApplyDamage(float amount, AttackHitContext context, bool hasContext)
    {
        if (isDead || amount <= 0f)
        {
            return;
        }

        if (hasContext)
        {
            lastHitContext = context;
            hasLastHitContext = true;
        }
        else
        {
            lastHitContext = default;
            hasLastHitContext = false;
        }

        if (currentShield > 0f)
        {
            float absorbed = Mathf.Min(amount, currentShield);
            currentShield -= absorbed;
            amount -= absorbed;
            Debug.Log($"{gameObject.name} absorbed {absorbed} damage with its shield. Remaining Shield: {currentShield}");
        }

        if (amount > 0f)
        {
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            Debug.Log($"{gameObject.name} took {amount} damage. Remaining Health: {currentHealth}, Shield: {currentShield}");
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        Debug.Log($"{gameObject.name} has died.");
        EnsureDeathEvent();
        OnDeath?.Invoke();

        switch (deathMode)
        {
            case HealthDeathMode.DisableGameObject:
                gameObject.SetActive(false);
                break;
            case HealthDeathMode.None:
                break;
            case HealthDeathMode.DestroyGameObject:
            default:
                Destroy(gameObject);
                break;
        }
    }

    private void EnsureDeathEvent()
    {
        if (OnDeath == null)
        {
            OnDeath = new UnityEvent();
        }
    }
}
