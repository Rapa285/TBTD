using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public enum HealthDeathMode
{
    DestroyGameObject = 0,
    DisableGameObject = 1,
    None = 2
}

/// <summary>
/// Basic health and shield receiver that supports both plain and context-aware damage.
/// </summary>
public class HealthComponent : MonoBehaviour, IAttackContextDamageable, IDamageable
{
    [Header("References")]
    [SerializeField] private EnemyAudio enemyAudio;
    [SerializeField] private GameObject shieldVFXObj;

    [Header("Auto Initialization")]
    [SerializeField, Tooltip("Initialize health automatically from the serialized starting values on Start.")]
    private bool initializeOnStart = false;

    [SerializeField, Min(1f), Tooltip("Starting and maximum health used when auto-initializing or when Initialize is called with this value.")]
    private float startingHealth = 100f;

    [SerializeField, Min(0f), Tooltip("Starting shield used when auto-initializing. Shield absorbs incoming damage before health.")]
    private float startingShield = 0f;

    [Header("Revive")]
    [SerializeField, Tooltip("Number of extra lives this entity has. When health reaches zero, it will be revived with full health until extra lives are exhausted. Shield is not restored on revive.")]
    private int extraLives = 0;

    [Header("Death")]
    [SerializeField, Tooltip("What happens to this GameObject after health reaches zero.")]
    private HealthDeathMode deathMode = HealthDeathMode.DestroyGameObject;

    private float maxHealth;
    private float currentHealth;
    private float maxShield;
    private float currentShield;
    private bool isDead;
    private bool hasLastHitContext;
    private AttackHitContext lastHitContext;
    private CancellationTokenSource lifeTokenSource;
    private CancellationToken activeLifeToken;

    [HideInInspector] public UnityEvent OnDeath = new UnityEvent();

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float MaxShield => maxShield;
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

    /// <summary>
    /// Resets current health, shield, and cached hit context.
    /// </summary>
    public void Initialize(float health, float shield = 0f)
    {
        lifeTokenSource?.Cancel();
        lifeTokenSource?.Dispose();
        lifeTokenSource=CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        activeLifeToken=lifeTokenSource.Token;

        maxHealth = Mathf.Max(1f, health);
        currentHealth = maxHealth;
        currentShield = Mathf.Max(0f, shield);
        maxShield = Mathf.Max(1f, currentShield);
        isDead = false;
        hasLastHitContext = false;
        lastHitContext = default;
        EnsureDeathEvent();

        UpdateShieldVisuals();
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

        float actualDamageTaken = amount;

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

        UpdateShieldVisuals();

        // damage popup
        DamagePopupVFX.GlobalPendingDamage = actualDamageTaken;        
        TryRequestVFX(VFXType.DamagePopup, transform, attach: false);

        if (currentHealth <= 0f)
        {
            if (extraLives > 0)
            {
                extraLives--;
                currentHealth = maxHealth;
                currentShield = 0f;
                Debug.Log($"{gameObject.name} has been revived! Remaining extra lives: {extraLives}");
                
                TryRequestVFX(VFXType.Revive, transform, attach: true);
                return;
            }
            Die();
        }
        else
        {
            if (enemyAudio != null)
            {
                enemyAudio.PlayHit();
            }

            TryRequestVFX(VFXType.BulletHit, transform, attach: false);
        }
    }

    private void TryRequestVFX(VFXType vfxType, Transform target, bool attach)
    {
        if (target == null || vfxType == VFXType.None)
        {
            return;
        }

        if (ServiceLocator.TryResolve<VFXService>(out VFXService vfxService) && vfxService != null)
        {
            vfxService.HandleRequest(vfxType, target, attach ? target : null, follow: attach);
        }
    }

    private void UpdateShieldVisuals()
    {
        if (shieldVFXObj != null)
        {
            shieldVFXObj.SetActive(currentShield > 0f && !isDead);
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        UpdateShieldVisuals();
        lifeTokenSource?.Cancel();
        Debug.Log($"{gameObject.name} has died.");
        EnsureDeathEvent();
        OnDeath?.Invoke();

        if (enemyAudio != null)
        {
            enemyAudio.PlayDeath();
        }
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

    public void ApplyTemporaryShieldBuff(float amount, float duration)
    {
        if (isDead) return;
        currentShield += amount;
        maxShield = Mathf.Max(maxShield, currentShield);
        Debug.Log($"{gameObject.name} received a temporary shield buff of {amount}. Current Shield: {currentShield}");
        
        UpdateShieldVisuals();
        _ = RemoveShieldBuffAfterDuration(amount, duration);
    }

    private async Awaitable RemoveShieldBuffAfterDuration(float amount, float duration)
    {
        try
        {
            await Awaitable.WaitForSecondsAsync(duration, activeLifeToken);

            if (!isDead && currentShield > 0f)
            {
                float amountToRemove = Mathf.Min(amount, currentShield);
                currentShield -= amountToRemove;
                Debug.Log($"{gameObject.name}'s temporary shield buff expired. Current Shield: {currentShield}");
                UpdateShieldVisuals();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}