using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual fill controller for a hold-to-recall button.
/// </summary>
public class RecallButtonFX : MonoBehaviour
{
    [SerializeField, Tooltip("Image whose fill amount represents recall hold progress.")]
    private Image fillImage;

    private float activeDuration;
    private bool isHolding;

    public float ActiveDuration => activeDuration;
    public bool IsHolding => isHolding;

    private void Awake()
    {
        ResolveReferences();
        ResetFill();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ResetFill();
    }

    private void OnDisable()
    {
        CancelHold();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void BeginHold(float duration)
    {
        activeDuration = Mathf.Max(0f, duration);
        isHolding = true;
        SetFill(0f);
        OnHoldStarted(activeDuration);
    }

    public void SetFill(float normalized)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(normalized);
        }
    }

    public void ResetFill()
    {
        SetFill(1f);
    }

    public void CancelHold()
    {
        if (isHolding)
        {
            OnHoldCancelled();
        }

        isHolding = false;
        activeDuration = 0f;
        ResetFill();
    }

    public void CompleteHold()
    {
        if (isHolding)
        {
            OnHoldCompleted();
        }

        isHolding = false;
        SetFill(1f);
    }

    protected virtual void OnHoldStarted(float duration)
    {
        // Future repeating sound hook.
    }

    protected virtual void OnHoldCancelled()
    {
        // Future repeating sound hook.
    }

    protected virtual void OnHoldCompleted()
    {
        // Future repeating sound hook.
    }

    private void ResolveReferences()
    {
        if (fillImage == null)
        {
            fillImage = GetComponent<Image>();
        }

        if (fillImage == null)
        {
            fillImage = GetComponentInChildren<Image>(true);
        }
    }
}
