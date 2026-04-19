using UnityEngine;

[System.Serializable]
public struct EnemyStats
{
    [Min(1f)] public float health;
    [Min(0f)] public float damage;
    [Min(0.1f)] public float movementSpeed;
    [Min(0f)] public float initialShield;
}

[RequireComponent(typeof(HealthComponent), typeof(EnemyMover))]
public class EnemyEntity : MonoBehaviour
{
    [Header("Stats")]
    public EnemyStats stats = new EnemyStats { health = 100f, damage = 10f, movementSpeed = 3f, initialShield = 0f };

    [Header("Targeting")]
    [SerializeField] private Transform baseTarget;

    [Header("Visuals")]
    [SerializeField] private Material enemyMaterial;
    [SerializeField] private MeshRenderer meshRenderer;

    private HealthComponent health;
    private EnemyMover mover;

    public Transform BaseTarget 
    { 
        get { return baseTarget; } 
        set { baseTarget = value; } 
    }

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
        mover = GetComponent<EnemyMover>();

        mover.OnReachEnd.AddListener(HandleReachBase);
    }

    private void Start()
    {
        health.Initialize(stats.health, stats.initialShield);
        mover.Initialize(stats.movementSpeed);

        if (meshRenderer != null && enemyMaterial != null)
        {
            meshRenderer.material = enemyMaterial;
        }
    }

    private void HandleReachBase()
    {
        if (baseTarget != null)
        {
            Debug.Log($"{gameObject.name} reached the base and is dealing {stats.damage} damage.");
            baseTarget.SendMessage("TakeDamage", stats.damage, SendMessageOptions.DontRequireReceiver);
        }
        
        Destroy(gameObject);
    }
}