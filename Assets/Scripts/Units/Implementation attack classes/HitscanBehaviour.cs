using System.Collections;
using UnityEngine;

/// <summary>
/// Immediate single-target damage with a short shrinking line-rendered trace.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public sealed class HitscanBehaviour : AttackBehaviour
{
    [SerializeField, Tooltip("Line renderer used to draw the temporary shot trace.")]
    private LineRenderer lineRenderer;

    [SerializeField, Tooltip("Optional muzzle transform used as the trace start. Falls back to this transform.")]
    private Transform firePoint;

    [SerializeField, Min(0f), Tooltip("How long the trace shrinks before disappearing.")]
    private float traceDuration = 0.12f;

    private Coroutine activeTrace;

    private void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        ConfigureLineRenderer();
    }

    private void OnValidate()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        traceDuration = Mathf.Max(0f, traceDuration);
        ConfigureLineRenderer();
    }

    protected override bool ExecuteAttack(Transform target, float damage)
    {
        if (target == null)
        {
            return false;
        }

        Vector3 start = firePoint != null ? firePoint.position : transform.position;
        Vector3 end = GetAimPoint(target);
        bool damageApplied = TryApplyDamage(target, damage);
        RenderTrace(start, end);
        return damageApplied;
    }

    private void RenderTrace(Vector3 start, Vector3 end)
    {
        if (lineRenderer == null)
        {
            return;
        }

        if (activeTrace != null)
        {
            StopCoroutine(activeTrace);
        }

        activeTrace = StartCoroutine(RenderTraceRoutine(start, end));
    }

    private IEnumerator RenderTraceRoutine(Vector3 start, Vector3 end)
    {
        lineRenderer.enabled = true;

        float elapsed = 0f;
        while (elapsed < traceDuration)
        {
            float normalizedTime = traceDuration > 0f ? Mathf.Clamp01(elapsed / traceDuration) : 1f;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, Vector3.Lerp(end, start, normalizedTime));

            elapsed += Time.deltaTime;
            yield return null;
        }

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, start);
        lineRenderer.enabled = false;
        activeTrace = null;
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
