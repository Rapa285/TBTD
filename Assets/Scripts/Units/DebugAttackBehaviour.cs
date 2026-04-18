using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DebugAttackBehaviour : AttackBehaviour
{
    [SerializeField, Tooltip("Line renderer used to draw the temporary debug beam.")]
    private LineRenderer lineRenderer;

    [SerializeField, Tooltip("Optional muzzle transform used as the beam start. Falls back to this transform.")]
    private Transform firePoint;

    [SerializeField, Min(0f), Tooltip("How long the debug beam remains visible after each attack.")]
    private float beamDuration = 1.5f;

    private Coroutine activeBeam;

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

        beamDuration = Mathf.Max(0f, beamDuration);
        ConfigureLineRenderer();
    }

    protected override void ExecuteAttack(Transform target, float damage)
    {
        TryApplyDamage(target, damage);

        if (activeBeam != null)
        {
            StopCoroutine(activeBeam);
        }

        activeBeam = StartCoroutine(RenderBeam(target));
    }

    private System.Collections.IEnumerator RenderBeam(Transform target)
    {
        if (lineRenderer == null || target == null)
        {
            yield break;
        }

        lineRenderer.enabled = true;

        float elapsed = 0f;
        while (elapsed < beamDuration && target != null)
        {
            Vector3 start = firePoint != null ? firePoint.position : transform.position;
            lineRenderer.SetPosition(0, start);
            // The beam endpoint follows the live target position plus AttackBehaviour's shared final aim offset.
            lineRenderer.SetPosition(1, GetAimPoint(target));

            elapsed += Time.deltaTime;
            yield return null;
        }

        lineRenderer.enabled = false;
        activeBeam = null;
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
