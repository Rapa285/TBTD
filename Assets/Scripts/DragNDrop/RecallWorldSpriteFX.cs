using UnityEngine;

/// <summary>
/// Programmatic sprite feedback for a world-space hold-to-recall control.
/// </summary>
public sealed class RecallWorldSpriteFX : MonoBehaviour
{
    [SerializeField, Tooltip("Root scaled by hover and hold feedback. Defaults to this transform.")]
    private Transform target;

    [SerializeField, Tooltip("Optional background sprite tinted with availability state.")]
    private SpriteRenderer backgroundRenderer;

    [SerializeField, Tooltip("Optional main icon sprite tinted and pulsed during hover/hold.")]
    private SpriteRenderer iconRenderer;

    [SerializeField, Tooltip("Optional progress sprite scaled on local X from empty to full.")]
    private SpriteRenderer progressRenderer;

    [SerializeField, Tooltip("Scale multiplier while pointer is hovering.")]
    private Vector3 hoverScale = Vector3.one * 1.08f;

    [SerializeField, Tooltip("Additional scale multiplier pulsed while recall is being held.")]
    private Vector3 holdPulseScale = Vector3.one * 1.14f;

    [SerializeField, Min(0f), Tooltip("Seconds used to smooth color and scale changes.")]
    private float smoothingTime = 0.08f;

    [SerializeField, Min(0f), Tooltip("Hold pulse cycles per second.")]
    private float holdPulseFrequency = 3f;

    [SerializeField, Tooltip("Tint used when available but not hovered.")]
    private Color normalTint = Color.white;

    [SerializeField, Tooltip("Tint used while hovered.")]
    private Color hoverTint = new Color(1f, 0.95f, 0.7f, 1f);

    [SerializeField, Tooltip("Tint used while hold-to-recall is active.")]
    private Color holdTint = new Color(1f, 0.55f, 0.35f, 1f);

    [SerializeField, Tooltip("Tint used while unavailable.")]
    private Color unavailableTint = new Color(1f, 1f, 1f, 0f);

    private Vector3 targetBaseScale = Vector3.one;
    private Vector3 progressBaseScale = Vector3.one;
    private bool isAvailable;
    private bool isHovered;
    private bool isHolding;
    private float holdElapsed;
    private bool capturedTargetBaseScale;
    private bool capturedProgressBaseScale;

    private void Awake()
    {
        ResolveReferences();
        CaptureBaseState();
        ApplyImmediate();
    }

    private void OnEnable()
    {
        ResolveReferences();
        CaptureBaseState();
        ApplyImmediate();
    }

    private void Update()
    {
        if (isHolding)
        {
            holdElapsed += Time.deltaTime;
        }

        ApplySmoothed();
    }

    private void OnValidate()
    {
        ResolveReferences();
        smoothingTime = Mathf.Max(0f, smoothingTime);
        holdPulseFrequency = Mathf.Max(0f, holdPulseFrequency);
    }

    public void SetAvailable(bool available)
    {
        isAvailable = available;
        if (!isAvailable)
        {
            isHovered = false;
            isHolding = false;
            holdElapsed = 0f;
            SetProgress(0f);
        }

        SetRenderersEnabled(isAvailable);
        ApplyImmediate();
    }

    public void SetHovered(bool hovered)
    {
        isHovered = isAvailable && hovered;
    }

    public void BeginHold(float duration)
    {
        isHolding = true;
        holdElapsed = 0f;
        SetProgress(0f);
    }

    public void SetHoldProgress(float normalizedProgress)
    {
        SetProgress(normalizedProgress);
    }

    public void CancelHold()
    {
        isHolding = false;
        holdElapsed = 0f;
        SetProgress(0f);
    }

    public void CompleteHold()
    {
        isHolding = false;
        SetProgress(1f);
    }

    private void ResolveReferences()
    {
        if (target == null)
        {
            target = transform;
        }

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (iconRenderer == null && renderers.Length > 0)
        {
            iconRenderer = renderers[0];
        }

        if (backgroundRenderer == null && renderers.Length > 1)
        {
            backgroundRenderer = renderers[0];
            iconRenderer = renderers[1];
        }

        if (progressRenderer == null && renderers.Length > 2)
        {
            progressRenderer = renderers[2];
        }
    }

    private void CaptureBaseState()
    {
        if (!capturedTargetBaseScale && target != null)
        {
            targetBaseScale = target.localScale;
            capturedTargetBaseScale = true;
        }

        if (!capturedProgressBaseScale && progressRenderer != null)
        {
            progressBaseScale = progressRenderer.transform.localScale;
            capturedProgressBaseScale = true;
        }
    }

    private void ApplySmoothed()
    {
        float t = smoothingTime > 0f
            ? Mathf.Clamp01(Time.deltaTime / smoothingTime)
            : 1f;

        ApplyScale(t);
        ApplyTint(t);
    }

    private void ApplyImmediate()
    {
        ApplyScale(1f);
        ApplyTint(1f);
    }

    private void ApplyScale(float t)
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredScale = targetBaseScale;
        if (isHolding)
        {
            float pulse = holdPulseFrequency > 0f
                ? (Mathf.Sin(holdElapsed * Mathf.PI * 2f * holdPulseFrequency) + 1f) * 0.5f
                : 1f;
            Vector3 pulseScale = Vector3.Lerp(hoverScale, holdPulseScale, pulse);
            desiredScale = MultiplyScale(targetBaseScale, pulseScale);
        }
        else if (isHovered)
        {
            desiredScale = MultiplyScale(targetBaseScale, hoverScale);
        }

        target.localScale = Vector3.Lerp(target.localScale, desiredScale, t);
    }

    private void ApplyTint(float t)
    {
        Color tint = unavailableTint;
        if (isHolding)
        {
            tint = holdTint;
        }
        else if (isHovered)
        {
            tint = hoverTint;
        }
        else if (isAvailable)
        {
            tint = normalTint;
        }

        LerpRendererColor(backgroundRenderer, tint, t);
        LerpRendererColor(iconRenderer, tint, t);
        LerpRendererColor(progressRenderer, holdTint, t);
    }

    private void SetProgress(float normalizedProgress)
    {
        if (progressRenderer == null)
        {
            return;
        }

        Vector3 scale = progressBaseScale;
        scale.x = progressBaseScale.x * Mathf.Clamp01(normalizedProgress);
        progressRenderer.transform.localScale = scale;
    }

    private void SetRenderersEnabled(bool enabled)
    {
        SetRendererEnabled(backgroundRenderer, enabled);
        SetRendererEnabled(iconRenderer, enabled);
        SetRendererEnabled(progressRenderer, enabled);
    }

    private static Vector3 MultiplyScale(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    private static void LerpRendererColor(SpriteRenderer spriteRenderer, Color tint, float t)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.color = Color.Lerp(spriteRenderer.color, tint, t);
    }

    private static void SetRendererEnabled(SpriteRenderer spriteRenderer, bool enabled)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = enabled;
        }
    }
}
