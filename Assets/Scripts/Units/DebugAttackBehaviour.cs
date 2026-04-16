using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DebugAttackBehaviour : AttackBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform firePoint;
    [SerializeField, Min(0f)] private float beamDuration = 1.5f;

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
            lineRenderer.SetPosition(1, target.position);

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
