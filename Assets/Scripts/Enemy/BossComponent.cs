using System.Collections;
using UnityEngine;

public class BossSetupComponent : MonoBehaviour
{
    [Header("Boss Settings")]
    [SerializeField] private float difficultyMultiplier = 8f;
    [SerializeField] private float sizeMultiplier = 2f;
    [SerializeField, Range(0.1f, 0.9f)] private float permanentSlowPercentage = 0.3f;
    private IEnumerator Start()
    {
        yield return null;
        MakeItBoss();
    }

    public void MakeItBoss()
    {
        transform.localScale *= sizeMultiplier;

        var scaler = GetComponent<DifficultyScaler>();
        if (scaler != null)
        {
            scaler.ApplyDifficulty(difficultyMultiplier);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} got no DifficultyScaler! Boss HP/Shield won't be inflated.");
        }

        var statusManager = GetComponent<StatusEffectManager>();
        if (statusManager != null)
        {
            SlowEffect permanentSlow = new SlowEffect(permanentSlowPercentage, Mathf.Infinity);
            statusManager.AddEffect(permanentSlow);
        }

        Debug.Log($"{gameObject.name} has been transformed into a BOSS!");        
    }
}