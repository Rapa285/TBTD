using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Events;
using System.Threading;

[RequireComponent(typeof(SplineAnimate))]
public class EnemyMover : MonoBehaviour
{
    [SerializeField] private EnemyAudio enemyAudio;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    [Header("VFX")]
    [SerializeField] private GameObject speedBuffVFXObj;
    private SplineAnimate splineAnimate;
    private float baseSpeed;
    private float auraBuffMultiplier = 1f;
    private float buffTimer = 0f;
    private bool hasReachedEnd;
    private System.Collections.Generic.List<float> speedFactors = new System.Collections.Generic.List<float>();

    public float BaseSpeed => baseSpeed;

    [HideInInspector] public UnityEvent OnReachEnd;

    public float CurrentSpeedMultiplier { get; private set; } = 1f;

    private void Awake()
    {
        splineAnimate = GetComponent<SplineAnimate>();
    }

    /// <summary>
    /// Will reset the enemy's movement state and initialize its speed
    /// </summary>
    public void Initialize(float speed)
    {
        baseSpeed = Mathf.Max(0.1f, speed);        
        auraBuffMultiplier = 1f;
        buffTimer = 0f;
        hasReachedEnd = false;
        speedFactors.Clear();
        
        if (speedBuffVFXObj != null) speedBuffVFXObj.SetActive(false);

        UpdateSpeed();
        
        if (splineAnimate != null)
        {
            splineAnimate.NormalizedTime = 0f;
            if (!splineAnimate.IsPlaying)
            {
                splineAnimate.Play();
            }
        }
    }

    public void ApplyTemporarySpeedBuff(float multiplier, float duration)
    {
        if (buffTimer < duration) 
        {
            buffTimer = duration;
        }
        
        auraBuffMultiplier = multiplier;
        
        if (speedBuffVFXObj != null) speedBuffVFXObj.SetActive(true);

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

            CurrentSpeedMultiplier = auraBuffMultiplier;

            for (int i = 0; i < speedFactors.Count; i++)
            {
                CurrentSpeedMultiplier *= speedFactors[i];
            }
            
            splineAnimate.MaxSpeed = baseSpeed * CurrentSpeedMultiplier;            
            splineAnimate.NormalizedTime = currentProgress;

            if (animator != null) animator.speed=CurrentSpeedMultiplier;
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
                
                if (speedBuffVFXObj != null) speedBuffVFXObj.SetActive(false);

                UpdateSpeed();
            }
        }

        if (!hasReachedEnd && splineAnimate != null && splineAnimate.NormalizedTime >= 1f)
        {
            hasReachedEnd = true;
            if (enemyAudio != null) enemyAudio.PlayAttackBase();
            
            OnReachEnd?.Invoke();
        }
    }

    public void PauseMovement()
    {
        if (splineAnimate != null) splineAnimate.Pause();
        if (animator != null) animator.speed = 0f;
    }

    public void ResumeMovement()
    {
        if (splineAnimate != null && !hasReachedEnd) splineAnimate.Play();
        if (animator != null) animator.speed = CurrentSpeedMultiplier;
    }

    public async Awaitable PauseForSeconds(float duration, CancellationToken token)
    {
        PauseMovement();
        try
        {
            await Awaitable.WaitForSecondsAsync(duration, token);
            ResumeMovement();
        }
        catch (System.OperationCanceledException)
        {
        }
    }
}