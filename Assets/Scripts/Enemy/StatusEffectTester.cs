using UnityEngine;

public class StatusEffectTester : MonoBehaviour
{
    [Header("Target References")]
    [SerializeField] private StatusEffectManager statusManager;
    [SerializeField] private HealthComponent health;

    private void Start()
    {
        if (statusManager == null) statusManager = GetComponent<StatusEffectManager>();
        if (health == null) health = GetComponent<HealthComponent>();
    }

    [ContextMenu("Test: Apply Poison (DOT)")]
    public void ApplyTestPoison()
    {
        if (statusManager == null) return;
        
        // 5 damage per tick, total 5x tick, setiap 1 detik
        var poison = new DotEffect(5f, 5, 1f); 
        statusManager.AddEffect(poison);
        
        Debug.Log("TESTER: Menembakkan efek Poison (DOT)!");
    }

    [ContextMenu("Test: Apply Slow")]
    public void ApplyTestSlow()
    {
        if (statusManager == null) return;
        
        // Slow 50% (0.5f) selama 4 detik
        var slow = new SlowEffect(0.5f, 4f);
        statusManager.AddEffect(slow);
        
        Debug.Log("TESTER: Menembakkan efek Slow!");
    }

    [ContextMenu("Test: Apply Stun")]
    public void ApplyTestStun()
    {
        if (statusManager == null) return;
        
        // Stun selama 3 detik
        var stun = new StunEffect(3f);
        statusManager.AddEffect(stun);
        
        Debug.Log("TESTER: Menembakkan efek Stun!");
    }

    [ContextMenu("Test: Add Temporary Shield")]
    public void ApplyTestShield()
    {
        if (health == null) return;
        
        // Tambah shield 30 poin selama 5 detik
        health.ApplyTemporaryShieldBuff(30f, 5f);
        
        Debug.Log("TESTER: Memberikan Shield Kuning!");
    }

    [ContextMenu("Test: Take Raw Damage")]
    public void ApplyTestDamage()
    {
        if (health == null) return;
        
        health.TakeDamage(15f);
        Debug.Log("TESTER: Memberikan 15 Damage!");
    }
}