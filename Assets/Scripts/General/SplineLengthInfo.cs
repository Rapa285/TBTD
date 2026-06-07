using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
[RequireComponent(typeof(SplineContainer))]
public class SplineLengthInfo : MonoBehaviour
{
    [Header("Panjang Total Spline (Unit/Meter)")]
    public float totalLength;

    void Update()
    {
        // Hanya menghitung saat di dalam Editor (tidak memberatkan performa saat game dimainkan)
        if (!Application.isPlaying) 
        {
            totalLength = GetComponent<SplineContainer>().CalculateLength();
        }
    }
}