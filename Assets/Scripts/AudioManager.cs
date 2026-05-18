using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer Routing")]
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup towerSfxGroup;
    [SerializeField] private AudioMixerGroup enemySfxGroup;

    [Header("Dedicated Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource towerSfxSource;
    [SerializeField] private AudioSource enemySfxSource;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Otomatis assign mixer group ke source jika belum diatur via Inspector
        if (bgmSource != null && bgmGroup != null) bgmSource.outputAudioMixerGroup = bgmGroup;
        if (towerSfxSource != null && towerSfxGroup != null) towerSfxSource.outputAudioMixerGroup = towerSfxGroup;
        if (enemySfxSource != null && enemySfxGroup != null) enemySfxSource.outputAudioMixerGroup = enemySfxGroup;
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;
        if (bgmSource.clip == clip) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void PlayTowerSFX(AudioClip clip, bool randomizePitch = true)
    {
        if (clip == null || towerSfxSource == null) return;

        towerSfxSource.pitch = randomizePitch ? Random.Range(0.85f, 1.15f) : 1f;
        towerSfxSource.PlayOneShot(clip);
    }

    public void PlayEnemySFX(AudioClip clip, bool randomizePitch = false)
    {
        if (clip == null || enemySfxSource == null) return;

        enemySfxSource.pitch = randomizePitch ? Random.Range(0.9f, 1.1f) : 1f;
        enemySfxSource.PlayOneShot(clip);
    }
}