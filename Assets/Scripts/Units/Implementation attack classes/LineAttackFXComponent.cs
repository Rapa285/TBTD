using System.Collections;
using UnityEngine;

/// <summary>
/// Shared line-rendered visual effect that draws a full line, then thins it out.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public sealed class LineAttackFXComponent : MonoBehaviour, AttackFXComponent
{
    [SerializeField, Tooltip("Line renderer used to display the beam.")]
    private LineRenderer lineRenderer;

    [SerializeField, Min(0f), Tooltip("Seconds the full line remains visible while thinning out.")]
    private float duration = 0.12f;

    [SerializeField, Min(0f), Tooltip("Line width multiplier when the visual starts.")]
    private float startWidthMultiplier = 1f;

    [SerializeField, Min(0f), Tooltip("Line width multiplier when the visual ends.")]
    private float endWidthMultiplier;

    [SerializeField, Min(0.01f), Tooltip("Ease-in exponent used when thinning the line. Higher values start thinning more slowly.")]
    private float widthEasePower = 2f;

    [SerializeField, Min(0f), Tooltip("Fallback endpoint distance used when no hit point or target is supplied.")]
    private float fallbackEndpointDistance = 10f;

    private Coroutine activeRoutine;
    private float cachedWidthMultiplier = 1f;
    private bool hasCachedWidthMultiplier;

    private void Awake()
    {
        ResolveLineRenderer();
        ConfigureLineRenderer();
    }

    private void OnValidate()
    {
        ResolveLineRenderer();
        duration = Mathf.Max(0f, duration);
        startWidthMultiplier = Mathf.Max(0f, startWidthMultiplier);
        endWidthMultiplier = Mathf.Max(0f, endWidthMultiplier);
        widthEasePower = Mathf.Max(0.01f, widthEasePower);
        fallbackEndpointDistance = Mathf.Max(0f, fallbackEndpointDistance);
        ConfigureLineRenderer();
    }

    private void OnDisable()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        HideLine();
    }

    public void PlayAttackFX(AttackFXContext context)
    {
        ResolveLineRenderer();
        if (lineRenderer == null)
        {
            return;
        }

        Vector3 start = context.HasOrigin ? context.Origin : transform.position;
        Vector3 end = ResolveEndPoint(context, start);

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(PlayLineFade(start, end));
    }

    public void PlaySustainedLine(Vector3 start, Vector3 end, float visibleDuration)
    {
        PlaySustainedLine(start, end, visibleDuration, 1f);
    }

    public void PlaySustainedLine(Vector3 start, Vector3 end, float visibleDuration, float widthScale)
    {
        ResolveLineRenderer();
        if (lineRenderer == null)
        {
            return;
        }

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(PlaySustainedLineRoutine(
            start,
            end,
            Mathf.Max(0f, visibleDuration),
            Mathf.Max(0f, widthScale)));
    }

    private IEnumerator PlayLineFade(Vector3 start, Vector3 end)
    {
        CacheWidthMultiplier();
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.widthMultiplier = startWidthMultiplier;

        float elapsed = 0f;
        while (duration > 0f && elapsed < duration)
        {
            SetWidthAtNormalizedTime(Mathf.Clamp01(elapsed / duration));

            elapsed += Time.deltaTime;
            yield return null;
        }

        SetWidthAtNormalizedTime(1f);
        yield return null;
        HideLine();
    }

    private IEnumerator PlaySustainedLineRoutine(Vector3 start, Vector3 end, float visibleDuration, float widthScale)
    {
        CacheWidthMultiplier();
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.widthMultiplier = startWidthMultiplier * widthScale;

        float elapsed = 0f;
        while (visibleDuration > 0f && elapsed < visibleDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        HideLine();
    }

    private void SetWidthAtNormalizedTime(float normalizedTime)
    {
        float easedTime = Mathf.Pow(Mathf.Clamp01(normalizedTime), widthEasePower);
        lineRenderer.widthMultiplier = Mathf.Lerp(startWidthMultiplier, endWidthMultiplier, easedTime);
    }

    private void CacheWidthMultiplier()
    {
        if (!hasCachedWidthMultiplier && lineRenderer != null)
        {
            cachedWidthMultiplier = lineRenderer.widthMultiplier;
            hasCachedWidthMultiplier = true;
        }
    }

    private Vector3 ResolveEndPoint(AttackFXContext context, Vector3 start)
    {
        if (context.HasHitPosition)
        {
            return context.HitPosition;
        }

        if (context.Target != null)
        {
            return context.Target.position;
        }

        Vector3 forward = transform.forward;
        if (forward.sqrMagnitude <= Mathf.Epsilon)
        {
            forward = Vector3.forward;
        }

        return start + forward.normalized * fallbackEndpointDistance;
    }

    private void HideLine()
    {
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, Vector3.zero);
            lineRenderer.enabled = false;
            if (hasCachedWidthMultiplier)
            {
                lineRenderer.widthMultiplier = cachedWidthMultiplier;
            }
        }

        activeRoutine = null;
    }

    private void ResolveLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }
    }

    private void ConfigureLineRenderer()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
    }
}
