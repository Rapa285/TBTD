// Base helper for attacks that need simple predictive aim.
// Concrete weapons still implement ExecuteAttack; this class only calculates aim points,
// derives target velocity from Rigidbody by default, and stores last-shot gizmo data.
using UnityEngine;

public abstract class LeadingAttackBehaviour : AttackBehaviour
{
    [SerializeField, Min(0f), Tooltip("Seconds of lead time added per Unity unit of horizontal distance to the target.")]
    private float distanceFactor = 0.05f;

    [SerializeField, Min(0f), Tooltip("Extra seconds added to projectile travel-time leading to compensate for frame and spawn latency.")]
    private float leadBiasSeconds;

    [SerializeField, Tooltip("When enabled, lead calculations ignore movement along the configured vertical axis.")]
    private bool ignoreVerticalAxis = true;

    [SerializeField, Tooltip("Axis treated as vertical when flattening lead calculations. Defaults to world up.")]
    private Vector3 verticalAxis = Vector3.up;

    [SerializeField, Tooltip("Draws the most recent enemy position, final aim point, and aim direction while this object is selected.")]
    private bool drawLeadGizmos = true;

    [SerializeField, Min(0f), Tooltip("Radius of the gizmo drawn at the target's unmodified position.")]
    private float enemyPositionGizmoRadius = 0.2f;

    [SerializeField, Min(0f), Tooltip("Radius of the gizmo drawn at the final aim point, including Aim Modifier Vector.")]
    private float aimPointGizmoRadius = 0.25f;

    [SerializeField, Tooltip("Color used for the target's unmodified position gizmo.")]
    private Color enemyPositionGizmoColor = Color.yellow;

    [SerializeField, Tooltip("Color used for the line from attack origin to final aim point.")]
    private Color leadDirectionGizmoColor = Color.cyan;

    [SerializeField, Tooltip("Color used for the final aim point gizmo after lead and Aim Modifier Vector are applied.")]
    private Color aimPointGizmoColor = Color.magenta;

    private bool hasLeadGizmoData;
    private Vector3 lastShotOrigin;
    private Vector3 lastEnemyPosition;
    private Vector3 lastAimPoint;

    public float DistanceFactor
    {
        get => distanceFactor;
        set => distanceFactor = Mathf.Max(0f, value);
    }

    public float LeadBiasSeconds
    {
        get => leadBiasSeconds;
        set => leadBiasSeconds = Mathf.Max(0f, value);
    }

    protected virtual void OnValidate()
    {
        distanceFactor = Mathf.Max(0f, distanceFactor);
        leadBiasSeconds = Mathf.Max(0f, leadBiasSeconds);

        if (verticalAxis.sqrMagnitude <= Mathf.Epsilon)
        {
            verticalAxis = Vector3.up;
        }
        else
        {
            verticalAxis.Normalize();
        }
    }

    protected virtual Vector3 GetLeadPosition(Transform target)
    {
        if (target == null)
        {
            return Vector3.zero;
        }

        Vector3 origin = GetAttackOrigin();
        Vector3 enemyPosition = target.position;
        Vector3 toTarget = target.position - origin;
        Vector3 leadPosition = target.position;

        if (ignoreVerticalAxis)
        {
            toTarget = Vector3.ProjectOnPlane(toTarget, verticalAxis);
        }

        float distance = toTarget.magnitude;
        if (distance <= Mathf.Epsilon)
        {
            leadPosition = ApplyAimModifier(leadPosition);
            RecordLeadGizmoData(origin, enemyPosition, leadPosition);
            return leadPosition;
        }

        if (TryGetTargetVelocity(target, out Vector3 targetVelocity))
        {
            if (ignoreVerticalAxis)
            {
                targetVelocity = Vector3.ProjectOnPlane(targetVelocity, verticalAxis);
            }

            Vector3 perpendicularVelocity = GetPerpendicularVelocity(targetVelocity, toTarget);
            float leadTime = distance * distanceFactor;

            leadPosition = target.position + perpendicularVelocity * leadTime;
        }

        // Apply the shared aim offset after predictive leading has selected the final raw aim point.
        leadPosition = ApplyAimModifier(leadPosition);
        RecordLeadGizmoData(origin, enemyPosition, leadPosition);
        return leadPosition;
    }

    protected virtual Vector3 GetLeadPosition(Transform target, float projectileSpeed)
    {
        if (target == null)
        {
            return Vector3.zero;
        }

        if (projectileSpeed <= Mathf.Epsilon)
        {
            return GetLeadPosition(target);
        }

        Vector3 origin = GetAttackOrigin();
        Vector3 predictedPosition = target.position;
        float travelTime = 0f;

        for (int i = 0; i < 3; i++)
        {
            Vector3 toPredictedTarget = predictedPosition - origin;
            if (ignoreVerticalAxis)
            {
                toPredictedTarget = Vector3.ProjectOnPlane(toPredictedTarget, verticalAxis);
            }

            float distance = toPredictedTarget.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                break;
            }

            travelTime = distance / projectileSpeed + leadBiasSeconds;
            if (!TryPredictTargetPositionAtTravelTime(target, travelTime, out predictedPosition))
            {
                return GetLeadPosition(target);
            }
        }

        return GetLeadPositionAtTravelTime(target, travelTime);
    }

    protected virtual Vector3 GetLeadPositionAtTravelTime(Transform target, float travelTime)
    {
        if (target == null)
        {
            return Vector3.zero;
        }

        Vector3 origin = GetAttackOrigin();
        Vector3 enemyPosition = target.position;
        Vector3 leadPosition = target.position;

        if (travelTime > Mathf.Epsilon
            && TryPredictTargetPositionAtTravelTime(target, travelTime, out Vector3 predictedPosition))
        {
            leadPosition = predictedPosition;
        }

        leadPosition = ApplyAimModifier(leadPosition);
        RecordLeadGizmoData(origin, enemyPosition, leadPosition);
        return leadPosition;
    }

    protected virtual Vector3 GetLeadDirection(Transform target)
    {
        if (target == null)
        {
            return GetDefaultForward();
        }

        Vector3 leadPosition = GetLeadPosition(target);
        Vector3 direction = leadPosition - GetAttackOrigin();

        if (ignoreVerticalAxis)
        {
            // Flatten the predictive direction first, then reapply the final aim offset unchanged.
            direction = Vector3.ProjectOnPlane(direction - AimModifierVector, verticalAxis) + AimModifierVector;
        }

        return direction.sqrMagnitude > Mathf.Epsilon
            ? direction.normalized
            : GetDefaultForward();
    }

    /// <summary>
    /// Override this if the attack should originate from a muzzle/socket instead of this transform.
    /// </summary>
    protected virtual Vector3 GetAttackOrigin()
    {
        return transform.position;
    }

    /// <summary>
    /// Default implementation tries to read Rigidbody velocity.
    /// Override this for custom movement systems.
    /// </summary>
    protected virtual bool TryGetTargetVelocity(Transform target, out Vector3 velocity)
    {
        velocity = Vector3.zero;

        if (target == null)
        {
            return false;
        }

        Rigidbody rb = target.GetComponentInParent<Rigidbody>();
        if (rb != null)
        {
            velocity = rb.linearVelocity;
            return true;
        }

        return false;
    }

    protected virtual bool TryPredictTargetPositionAtTravelTime(
        Transform target,
        float travelTime,
        out Vector3 predictedPosition)
    {
        predictedPosition = target != null ? target.position : Vector3.zero;

        if (target == null || travelTime <= Mathf.Epsilon)
        {
            return target != null;
        }

        if (!TryGetTargetVelocity(target, out Vector3 targetVelocity))
        {
            return false;
        }

        if (ignoreVerticalAxis)
        {
            targetVelocity = Vector3.ProjectOnPlane(targetVelocity, verticalAxis);
        }

        predictedPosition = target.position + targetVelocity * travelTime;
        return true;
    }

    protected virtual Vector3 GetPerpendicularVelocity(Vector3 velocity, Vector3 toTarget)
    {
        if (toTarget.sqrMagnitude <= Mathf.Epsilon)
        {
            return Vector3.zero;
        }

        Vector3 toTargetDirection = toTarget.normalized;
        Vector3 radialVelocity = Vector3.Project(velocity, toTargetDirection);
        return velocity - radialVelocity;
    }

    protected virtual Vector3 GetDefaultForward()
    {
        Vector3 forward = transform.forward;
        return forward.sqrMagnitude > Mathf.Epsilon
            ? forward.normalized
            : Vector3.forward;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (!drawLeadGizmos || !hasLeadGizmoData)
        {
            return;
        }

        Color previousColor = Gizmos.color;

        Gizmos.color = enemyPositionGizmoColor;
        Gizmos.DrawWireSphere(lastEnemyPosition, enemyPositionGizmoRadius);

        Gizmos.color = leadDirectionGizmoColor;
        Gizmos.DrawLine(lastShotOrigin, lastAimPoint);

        Gizmos.color = aimPointGizmoColor;
        Gizmos.DrawWireSphere(lastAimPoint, aimPointGizmoRadius);

        Gizmos.color = previousColor;
    }

    private void RecordLeadGizmoData(Vector3 shotOrigin, Vector3 enemyPosition, Vector3 aimPoint)
    {
        lastShotOrigin = shotOrigin;
        lastEnemyPosition = enemyPosition;
        lastAimPoint = aimPoint;
        hasLeadGizmoData = true;
    }
}
