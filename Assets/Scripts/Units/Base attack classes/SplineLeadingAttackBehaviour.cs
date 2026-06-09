// Leading helper for enemies moved by Unity Splines.
// It predicts timed shots by evaluating the target's future position along the spline path,
// and keeps tangent velocity as a fallback for the older distance-factor aiming model.
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public abstract class SplineLeadingAttackBehaviour : LeadingAttackBehaviour
{
    [SerializeField, Min(SplineUtility.PickResolutionMin), Tooltip("Sampling resolution used when finding the target's nearest point on its spline.")]
    private int nearestPointResolution = SplineUtility.PickResolutionDefault;

    [SerializeField, Range(1, 10), Tooltip("Refinement passes used when finding the target's nearest point on its spline.")]
    private int nearestPointIterations = 2;

    protected override void OnValidate()
    {
        base.OnValidate();
        nearestPointResolution = Mathf.Max(SplineUtility.PickResolutionMin, nearestPointResolution);
        nearestPointIterations = Mathf.Clamp(nearestPointIterations, 1, 10);
    }

    protected override bool TryGetTargetVelocity(Transform target, out Vector3 velocity)
    {
        if (TryGetSplineVelocity(target, out velocity))
        {
            return true;
        }

        return base.TryGetTargetVelocity(target, out velocity);
    }

    protected override bool TryPredictTargetPositionAtTravelTime(
        Transform target,
        float travelTime,
        out Vector3 predictedPosition)
    {
        if (TryGetSplinePositionAtTravelTime(target, travelTime, out predictedPosition))
        {
            return true;
        }

        return base.TryPredictTargetPositionAtTravelTime(target, travelTime, out predictedPosition);
    }

    private bool TryGetSplineVelocity(Transform target, out Vector3 velocity)
    {
        velocity = Vector3.zero;

        if (target == null || !TryGetSplineAnimate(target, out SplineAnimate splineAnimate))
        {
            return false;
        }

        SplineContainer container = splineAnimate.Container;
        if (container == null || container.Splines == null || container.Splines.Count == 0)
        {
            return false;
        }

        SplinePath<Spline> splinePath = new SplinePath<Spline>(container.Splines);
        if (splinePath.Count < 2)
        {
            return false;
        }

        NativeSpline nativeSpline = new NativeSpline(
            splinePath,
            container.transform.localToWorldMatrix,
            Allocator.Temp);

        try
        {
            float pathLength = nativeSpline.GetLength();
            if (nativeSpline.Count < 2 || pathLength <= Mathf.Epsilon)
            {
                return false;
            }

            SplineUtility.GetNearestPoint(
                nativeSpline,
                ToFloat3(target.position),
                out _,
                out float currentT,
                nearestPointResolution,
                nearestPointIterations);

            Vector3 tangent = ToVector3(nativeSpline.EvaluateTangent(currentT));
            if (tangent.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            float traversalSpeed = GetTraversalSpeed(splineAnimate, pathLength);
            if (traversalSpeed <= Mathf.Epsilon)
            {
                return false;
            }

            velocity = tangent.normalized * GetTraversalDirectionSign(splineAnimate) * traversalSpeed;
            return true;
        }
        finally
        {
            nativeSpline.Dispose();
        }
    }

    private bool TryGetSplinePositionAtTravelTime(Transform target, float travelTime, out Vector3 predictedPosition)
    {
        predictedPosition = target != null ? target.position : Vector3.zero;

        if (target == null || travelTime <= Mathf.Epsilon || !TryGetSplineAnimate(target, out SplineAnimate splineAnimate))
        {
            return target != null;
        }

        SplineContainer container = splineAnimate.Container;
        if (container == null || container.Splines == null || container.Splines.Count == 0)
        {
            return false;
        }

        SplinePath<Spline> splinePath = new SplinePath<Spline>(container.Splines);
        if (splinePath.Count < 2)
        {
            return false;
        }

        NativeSpline nativeSpline = new NativeSpline(
            splinePath,
            container.transform.localToWorldMatrix,
            Allocator.Temp);

        try
        {
            float pathLength = nativeSpline.GetLength();
            if (nativeSpline.Count < 2 || pathLength <= Mathf.Epsilon)
            {
                return false;
            }

            SplineUtility.GetNearestPoint(
                nativeSpline,
                ToFloat3(target.position),
                out _,
                out float currentT,
                nearestPointResolution,
                nearestPointIterations);

            float traversalSpeed = GetTraversalSpeed(splineAnimate, pathLength);
            if (traversalSpeed <= Mathf.Epsilon)
            {
                return false;
            }

            float normalizedDistance = traversalSpeed * travelTime / pathLength;
            float futureT = GetFutureSplineT(splineAnimate, currentT, normalizedDistance);
            predictedPosition = ToVector3(nativeSpline.EvaluatePosition(futureT));
            return true;
        }
        finally
        {
            nativeSpline.Dispose();
        }
    }

    private bool TryGetSplineAnimate(Transform target, out SplineAnimate splineAnimate)
    {
        splineAnimate = target.GetComponent<SplineAnimate>();
        if (splineAnimate != null)
        {
            return true;
        }

        splineAnimate = target.GetComponentInParent<SplineAnimate>();
        if (splineAnimate != null)
        {
            return true;
        }

        splineAnimate = target.GetComponentInChildren<SplineAnimate>();
        return splineAnimate != null;
    }

    private static float GetTraversalSpeed(SplineAnimate splineAnimate, float pathLength)
    {
        if (splineAnimate.MaxSpeed > Mathf.Epsilon)
        {
            return splineAnimate.MaxSpeed;
        }

        if (splineAnimate.Duration > Mathf.Epsilon)
        {
            return pathLength / splineAnimate.Duration;
        }

        return 0f;
    }

    private static float GetTraversalDirectionSign(SplineAnimate splineAnimate)
    {
        if (splineAnimate.Loop != SplineAnimate.LoopMode.PingPong)
        {
            return 1f;
        }

        int completedTraversals = Mathf.FloorToInt(splineAnimate.NormalizedTime);
        return (completedTraversals & 1) == 0 ? 1f : -1f;
    }

    private static float GetFutureSplineT(SplineAnimate splineAnimate, float currentT, float normalizedDistance)
    {
        if (splineAnimate.Loop == SplineAnimate.LoopMode.PingPong)
        {
            float directedT = currentT + normalizedDistance * GetTraversalDirectionSign(splineAnimate);
            float repeatedT = Mathf.Repeat(directedT, 2f);
            return repeatedT <= 1f ? repeatedT : 2f - repeatedT;
        }

        if (splineAnimate.Loop == SplineAnimate.LoopMode.Loop)
        {
            return Mathf.Repeat(currentT + normalizedDistance, 1f);
        }

        return Mathf.Clamp01(currentT + normalizedDistance);
    }

    private static float3 ToFloat3(Vector3 value)
    {
        return new float3(value.x, value.y, value.z);
    }

    private static Vector3 ToVector3(float3 value)
    {
        return new Vector3(value.x, value.y, value.z);
    }
}
