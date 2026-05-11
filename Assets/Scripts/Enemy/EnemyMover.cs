using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Events;

[RequireComponent(typeof(SplineAnimate))]
public class EnemyMover : MonoBehaviour
{
    private SplineAnimate splineAnimate;
    private float baseSpeed;
    private float auraBuffMultiplier = 1f;
    private float buffTimer = 0f;
    private bool hasReachedEnd;
    private System.Collections.Generic.List<float> speedFactors = new System.Collections.Generic.List<float>();

    public float BaseSpeed => baseSpeed;

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
        
        auraBuffMultiplier = multiplier;
        UpdateSpeed();
    }

    public void AddSpeedFactor(float factor)
    {
        speedFactors.Add(factor);
        UpdateSpeed();
    }

    public void RemoveSpeedFactor(float factor)
    {
        speedFactors.Remove(factor);
        UpdateSpeed();
    }

    private void UpdateSpeed()
    {
        if (splineAnimate != null)
        { 
            float currentProgress = splineAnimate.NormalizedTime;
            float totalMultiplier=auraBuffMultiplier;

            for (int i = 0; i < speedFactors.Count; i++)
            {
                totalMultiplier *= speedFactors[i];
            }
            
            splineAnimate.MaxSpeed = baseSpeed * totalMultiplier;            
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
                auraBuffMultiplier = 1f;
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