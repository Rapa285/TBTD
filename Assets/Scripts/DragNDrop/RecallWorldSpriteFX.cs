using UnityEngine;

/// <summary>
/// Programmatic sprite feedback for a world-space hold-to-recall control.
/// </summary>
public sealed class RecallWorldSpriteFX : MonoBehaviour
{
    [SerializeField, Tooltip("Root scaled only while hovered. Defaults to this transform.")]
    private Transform target;

    [SerializeField, Tooltip("Optional background sprite shown behind the fill.")]
    private SpriteRenderer backgroundRenderer;

    [SerializeField, Tooltip("Optional fill sprite shown in front of the background. Its local X scale is driven from 0 to its authored full size while holding.")]
    private SpriteRenderer progressRenderer;

    [SerializeField, Tooltip("Scale multiplier while pointer is hovering.")]
    private Vector3 hoverScale = Vector3.one * 1.08f;

    [SerializeField, Min(0f), Tooltip("Seconds used to smooth color and scale changes.")]
    private float smoothingTime = 0.08f;

    [SerializeField, Tooltip("Tint used when available but not hovered.")]
    private Color normalTint = Color.white;

    [SerializeField, Tooltip("Tint used while hovered.")]
    private Color hoverTint = new Color(1f, 0.95f, 0.7f, 1f);

    [SerializeField, Tooltip("Tint used by the fill sprite.")]
    private Color fillTint = new Color(1f, 0.55f, 0.35f, 1f);

    [SerializeField, Tooltip("Tint used while unavailable.")]
    private Color unavailableTint = new Color(1f, 1f, 1f, 0f);

    private Vector3 targetBaseScale = Vector3.one;
    private Vector3 progressBaseScale = Vector3.one;
    private bool isAvailable;
    private bool isHovered;
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
        ApplySmoothed();
    }

    private void OnValidate()
    {
        ResolveReferences();
        smoothingTime = Mathf.Max(0f, smoothingTime);
    }

    public void SetAvailable(bool available)
    {
        isAvailable = available;
        if (!isAvailable)
        {
            isHovered = false;
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
        SetProgress(0f);
    }

    public void SetHoldProgress(float normalizedProgress)
    {
        SetProgress(normalizedProgress);
    }

    public void CancelHold()
    {
        SetProgress(0f);
    }

    public void CompleteHold()
    {
        SetProgress(1f);
    }

    private void ResolveReferences()
    {
        if (target == null)
        {
            target = transform;
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
        if (isHovered)
        {
            desiredScale = MultiplyScale(targetBaseScale, hoverScale);
        }

        target.localScale = Vector3.Lerp(target.localScale, desiredScale, t);
    }

    private void ApplyTint(float t)
    {
        Color tint = unavailableTint;
        if (isHovered)
        {
            tint = hoverTint;
        }
        else if (isAvailable)
        {
            tint = normalTint;
        }

        LerpRendererColor(backgroundRenderer, tint, t);
        LerpRendererColor(progressRenderer, isAvailable ? fillTint : unavailableTint, t);
    }

    private void SetProgress(float normalizedProgress)
    {
        if (progressRenderer == null)
        {
            return;
        }

        if (!capturedProgressBaseScale)
        {
            progressBaseScale = progressRenderer.transform.localScale;
            capturedProgressBaseScale = true;
        }

        Vector3 scale = progressBaseScale;
        scale.x = progressBaseScale.x * Mathf.Clamp01(normalizedProgress);
        progressRenderer.transform.localScale = scale;
    }

    private void SetRenderersEnabled(bool enabled)
    {
        SetRendererEnabled(backgroundRenderer, enabled);
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
