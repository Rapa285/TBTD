using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Projectile that follows a fixed visual arc, then explodes at its trajectory endpoint.
/// </summary>
public sealed class ArchingBullet : BaseProjectile
{
    [SerializeField, Min(0f), Tooltip("Explosion radius used when the arc reaches its destination.")]
    private float explosionRadius = 2.5f;

    [SerializeField, Min(0f), Tooltip("Lowest arc height used for far targets.")]
    private float minArcHeight = 1f;

    [SerializeField, Min(0f), Tooltip("Highest arc height used for close targets.")]
    private float maxArcHeight = 4f;

    [SerializeField, Min(0.01f), Tooltip("Horizontal distance at or beyond which the projectile uses minimum arc height.")]
    private float distanceForMinHeight = 10f;

    [SerializeField, Min(0.01f), Tooltip("Minimum time in seconds before the projectile lands.")]
    private float minFlightDuration = 0.35f;

    [SerializeField, Min(0.01f), Tooltip("Maximum time in seconds before the projectile lands.")]
    private float maxFlightDuration = 1.25f;

    [SerializeField, Min(0.001f), Tooltip("Seconds of flight added per Unity unit of horizontal travel.")]
    private float flightTimePerUnit = 0.06f;

    private Vector3 startPosition;
    private Vector3 destination;
    private float arcHeight;
    private float flightDuration;
    private float elapsedFlightTime;
    private bool hasDestination;
    private bool exploded;

    protected override void OnValidate()
    {
        base.OnValidate();
        explosionRadius = Mathf.Max(0f, explosionRadius);
        minArcHeight = Mathf.Max(0f, minArcHeight);
        maxArcHeight = Mathf.Max(minArcHeight, maxArcHeight);
        distanceForMinHeight = Mathf.Max(0.01f, distanceForMinHeight);
        minFlightDuration = Mathf.Max(0.01f, minFlightDuration);
        maxFlightDuration = Mathf.Max(minFlightDuration, maxFlightDuration);
        flightTimePerUnit = Mathf.Max(0.001f, flightTimePerUnit);
    }

    protected override void OnTriggerEnter(Collider other)
    {
        // Grenades resolve hits only at trajectory completion, not through physical trigger contact.
    }

    public void SetDestination(Vector3 worldDestination)
    {
        startPosition = transform.position;
        destination = worldDestination;

        Vector3 horizontalOffset = Vector3.ProjectOnPlane(destination - startPosition, Vector3.up);
        float horizontalDistance = horizontalOffset.magnitude;
        float distanceRatio = Mathf.Clamp01(horizontalDistance / distanceForMinHeight);
        arcHeight = Mathf.Lerp(maxArcHeight, minArcHeight, distanceRatio);
        flightDuration = Mathf.Clamp(horizontalDistance * flightTimePerUnit, minFlightDuration, maxFlightDuration);
        elapsedFlightTime = 0f;
        hasDestination = true;

        Vector3 lookDirection = destination - startPosition;
        if (lookDirection.sqrMagnitude > Mathf.Epsilon)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }

    public override bool ReadyToFire()
    {
        return base.ReadyToFire() && hasDestination && flightDuration > 0f;
    }

    public override void Fire()
    {
        if (!ReadyToFire())
        {
            return;
        }

        exploded = false;
        base.Fire();
    }

    protected override void TickProjectile(float deltaTime)
    {
        if (!hasDestination)
        {
            return;
        }

        elapsedFlightTime += deltaTime;
        float normalizedTime = Mathf.Clamp01(elapsedFlightTime / flightDuration);
        Vector3 nextPosition = Vector3.Lerp(startPosition, destination, normalizedTime);
        nextPosition.y += Mathf.Sin(normalizedTime * Mathf.PI) * arcHeight;
        transform.position = nextPosition;

        if (normalizedTime >= 1f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (exploded)
        {
            return;
        }

        exploded = true;
        HashSet<Transform> damagedTargets = new HashSet<Transform>();
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            explosionRadius,
            HitLayers,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i];
            Transform target = ColliderTargetUtility.GetTargetTransform(hitCollider);
            if (target == null || !damagedTargets.Add(target))
            {
                continue;
            }

            ApplyProjectileHit(hitCollider, target, Damage);
        }

        RaiseBulletHit(transform);
        Expire();
    }
}
