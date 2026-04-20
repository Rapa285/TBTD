using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Events;

[RequireComponent(typeof(SplineAnimate))]
public class EnemyMover : MonoBehaviour
{
    private SplineAnimate splineAnimate;
    private float baseSpeed;
    private float speedMultiplier = 1f;
    private float buffTimer = 0f;
    private bool hasReachedEnd;

    [HideInInspector] public UnityEvent OnReachEnd;

    private void Awake()
    {
        splineAnimate = GetComponent<SplineAnimate>();
    }

    public void Initialize(float speed)
    {
        baseSpeed = Mathf.Max(0.1f, speed);
        UpdateSpeed();
        
        if (splineAnimate != null && !splineAnimate.IsPlaying)
        {
            splineAnimate.Play();
        }
    }

    public void ApplyTemporarySpeedBuff(float multiplier, float duration)
    {
        if (buffTimer < duration) 
        {
            buffTimer = duration;
        }
        
        speedMultiplier = multiplier;
        UpdateSpeed();
    }

    // --- PERBAIKAN DI SINI ---
    private void UpdateSpeed()
    {
        if (splineAnimate != null)
        {
            float currentProgress = splineAnimate.NormalizedTime;
            splineAnimate.MaxSpeed = baseSpeed * speedMultiplier;            
            splineAnimate.NormalizedTime = currentProgress;
        }
    }

    private void Update()
    {
        // Handle penghapusan buff otomatis
        if (buffTimer > 0)
        {
            buffTimer -= Time.deltaTime;
            if (buffTimer <= 0)
            {
                speedMultiplier = 1f;
                UpdateSpeed();
            }
        }

        if (!hasReachedEnd && splineAnimate != null && splineAnimate.NormalizedTime >= 1f)
        {
            hasReachedEnd = true;
            OnReachEnd?.Invoke();
        }
    }

    public void PauseMovement()
    {
        if (splineAnimate != null) splineAnimate.Pause();
    }

    public void ResumeMovement()
    {
        if (splineAnimate != null && !hasReachedEnd) splineAnimate.Play();
    }
}