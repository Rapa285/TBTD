using UnityEngine;

public class DifficultyScaler : MonoBehaviour
{
    [SerializeField] private HealthComponent health;
    [SerializeField] private EnemyMover mover;

    private void Start()
    {
        if (health == null)
        {
            health = GetComponent<HealthComponent>();
        }
        if (mover == null)
        {
            mover = GetComponent<EnemyMover>();
        }
    }
    
    public void ApplyDifficulty(float multiplier)
    {
        if (health != null)
        {
            float scaledHP=health.MaxHealth*multiplier;
            float scaledShield=health.MaxShield*multiplier;

            health.Initialize(scaledHP, scaledShield);
        }
        if (mover != null)
        {
            float speedBonus=1f+((multiplier-1f)*0.1f);
            mover.Initialize(mover.BaseSpeed*speedBonus);
        }
        Debug.Log($"Applied difficulty multiplier {multiplier} to {gameObject.name}. New HP: {health.MaxHealth}, New Shield: {health.MaxShield}");
    }
}
