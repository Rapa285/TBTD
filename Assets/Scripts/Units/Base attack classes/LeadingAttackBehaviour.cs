// Base helper for attacks that need simple predictive aim.
// Concrete weapons still implement ExecuteAttack; this class only calculates aim points,
// derives target velocity from Rigidbody by default, and stores last-shot gizmo data.
using UnityEngine;

public abstract class LeadingAttackBehaviour : AttackBehaviour
{
    [SerializeField, Min(0f)]
    private float distanceFactor = 0.05f;

    [SerializeField]
    private bool ignoreVerticalAxis = true;

    [SerializeField]
    private Vector3 verticalAxis = Vector3.up;

    [SerializeField] private bool drawLeadGizmos = true;
    [SerializeField, Min(0f)] private float enemyPositionGizmoRadius = 0.2f;
    [SerializeField, Min(0f)] private float aimPointGizmoRadius = 0.25f;
    [SerializeField] private Color enemyPositionGizmoColor = Color.yellow;
    [SerializeField] private Color leadDirectionGizmoColor = Color.cyan;
    [SerializeField] private Color aimPointGizmoColor = Color.magenta;

    private bool hasLeadGizmoData;
    private Vector3 lastShotOrigin;
    private Vector3 lastEnemyPosition;
    private Vector3 lastAimPoint;

    public float DistanceFactor
    {
        get => distanceFactor;
        set => distanceFactor = Mathf.Max(0f, value);
    }

    protected virtual void OnValidate()
    {
        distanceFactor = Mathf.Max(0f, distanceFactor);

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
            Debug.Log("Epsilon point hit");
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
            direction = Vector3.ProjectOnPlane(direction, verticalAxis);
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
