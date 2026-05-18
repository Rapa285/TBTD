using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GlowEffectUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Masukkan komponen Image yang ingin dianimasikan. Jika kosong, script akan mencari komponen Image di GameObject ini.")]
    public Image targetImage;

    [Header("Glow Settings")]
    [Tooltip("Warna saat Image sedang menyala (glowing).")]
    public Color glowColor = Color.white; 
    
    [Tooltip("Durasi transisi dari warna asli ke warna glow (dalam detik).")]
    public float duration = 1f; 

    private Color originalColor;

    void Start()
    {
        // Ambil komponen Image secara otomatis jika belum di-assign di Inspector
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        if (targetImage != null)
        {
            // Simpan warna awal image
            originalColor = targetImage.color;
            StartGlowing();
        }
        else
        {
            Debug.LogWarning("Komponen UI Image tidak ditemukan!");
        }
    }

    void StartGlowing()
    {
        // Menganimasikan warna ke glowColor
        targetImage.DOColor(glowColor, duration)
            .SetLoops(-1, LoopType.Yoyo) // -1 berarti infinite loop, Yoyo membuat animasi bolak-balik
            .SetEase(Ease.InOutSine);    // InOutSine membuat pergerakan pendaran terasa natural dan halus
    }

    void OnDestroy()
    {
        // Sangat penting: Matikan animasi DOTween saat object hancur untuk mencegah error
        if (targetImage != null)
        {
            targetImage.DOKill();
        }
    }
}