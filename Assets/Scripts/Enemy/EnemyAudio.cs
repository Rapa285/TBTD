using UnityEngine;

public class EnemyAudio : MonoBehaviour
{
    [Header("SFX Clips")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip attackBaseSound;
    
    [Header("Skill SFX")]
    [SerializeField] private AudioClip skillSound;

    // Dipanggil oleh HealthComponent saat HP berkurang
    public void PlayHit()
    {
        if (hitSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemySFX(hitSound);
        }
    }

    // Dipanggil oleh HealthComponent saat HP mencapai 0
    public void PlayDeath()
    {
        if (deathSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemySFX(deathSound);
        }
    }

    // Dipanggil oleh EnemyMover saat mencapai akhir jalur
    public void PlayAttackBase()
    {
        if (attackBaseSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemySFX(attackBaseSound);
        }
    }

    public void PlaySkill()
    {
        if (skillSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemySFX(skillSound);
        }
    }
}