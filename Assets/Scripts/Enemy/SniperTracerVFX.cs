using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class SniperTracerVFX : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lingerDuration = 0.2f; // Seberapa lama garis bertahan
    [SerializeField] private AnimationCurve alphaFadeCurve; // Kurva untuk transparansi

    private LineRenderer line;
    private Coroutine fadeRoutine;
    private Color originalStartColor;
    private Color originalEndColor;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        // Simpan warna asli agar bisa di-reset saat spawn ulang
        originalStartColor = line.startColor;
        originalEndColor = line.endColor;
        
        // Setup default kurva jika lupa diisi di Inspector
        if (alphaFadeCurve.length == 0)
        {
            alphaFadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        }
    }

    /// <summary>
    /// Dipanggil oleh Tower untuk menyalakan garis instan
    /// </summary>
    public void SetupTracer(Vector3 startPos, Vector3 endPos)
    {
        gameObject.SetActive(true); // Nyalakan objek (keluar dari pool)
        
        // Gambar garis
        line.positionCount = 2;
        line.SetPosition(0, startPos);
        line.SetPosition(1, endPos);

        // Hentikan fade lama jika ada, lalu mulai fade baru
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < lingerDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / lingerDuration; // Angka 0 - 1

            // Baca kurva alpha (1 = solid, 0 = hilang)
            float alphaValue = alphaFadeCurve.Evaluate(normalizedTime);

            // Terapkan ke warna garis
            SetLineAlpha(alphaValue);

            yield return null;
        }

        // Matikan objek (kembalikan ke pool) setelah fade selesai
        gameObject.SetActive(false); 
    }

    private void SetLineAlpha(float alpha)
    {
        // Ubah alpha start color
        Color sC = originalStartColor;
        sC.a = alpha;
        line.startColor = sC;

        // Ubah alpha end color
        Color eC = originalEndColor;
        eC.a = alpha;
        line.endColor = eC;
    }
}