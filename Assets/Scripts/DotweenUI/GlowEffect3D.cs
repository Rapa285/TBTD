using UnityEngine;
using DG.Tweening;

public class GlowEffect3D : MonoBehaviour
{
    [Header("Highlight Settings")]
    public Color glowColor = Color.yellow;
    public float duration = 1f;
    
    // Angka untuk membuat glow lebih terang (intensity)
    public float glowIntensity = 2f; 

    public Material objectMaterial;
    public Renderer objectRenderer;
    private Color originalEmission;

    // void Start()
    // {
    //     // Mendapatkan material dari komponen Renderer di objek 3D ini
    //     Renderer rend = GetComponent<Renderer>();
    //     if (rend != null)
    //     {
    //         // Ingat: .material akan membuat instance material baru khusus untuk objek ini
    //         objectMaterial = rend.material; 
            
    //         // Wajib: Mengaktifkan fitur Emission pada material via script
    //         objectMaterial.EnableKeyword("_EMISSION");
            
    //         StartHighlight();
    //     }
    // }

    void StartHighlight()
    {
        // Menganimasikan properti "_EmissionColor" menggunakan DOTween
        // Kita kalikan warna dengan intensity agar glow-nya terlihat menyala
        objectMaterial.DOColor(glowColor * glowIntensity, "_EmissionColor", duration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    void OnDestroy()
    {
        // Bersihkan tween dan hindari memory leak
        if (objectMaterial != null)
        {
            objectMaterial.DOKill();
        }
    }
}