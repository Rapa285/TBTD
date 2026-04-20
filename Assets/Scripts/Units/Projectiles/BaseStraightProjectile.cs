// Straight-line projectile implementation.
// Call SetDirection with a world aim point, then Fire; movement starts only after the
// projectile has a valid direction and BaseProjectile has accepted the shot.
using UnityEngine;

public class BaseStraightProjectile : BaseProjectile
{
    [SerializeField, Min(0f)] private float bulletSpeed = 10f;

    private Vector3 travelDirection = Vector3.forward;
    private bool hasDirection;

    public float BulletSpeed
    {
        get => bulletSpeed;
        set => bulletSpeed = Mathf.Max(0f, value);
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        bulletSpeed = Mathf.Max(0f, bulletSpeed);
    }

    public override bool ReadyToFire()
    {
        return base.ReadyToFire() && hasDirection && bulletSpeed > 0f;
    }

    public void SetDirection(Vector3 endpoint)
    {
        SetTravelDirection(endpoint - transform.position);
    }

    public void SetTravelDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            hasDirection = false;
            return;
        }

        travelDirection = direction.normalized;
        hasDirection = true;
        transform.rotation = Quaternion.LookRotation(travelDirection, Vector3.up);
    }

    public override void Fire()
    {
        if (!hasDirection)
        {
            SetTravelDirection(transform.forward);
        }

        base.Fire();
    }

    protected override void TickProjectile(float deltaTime)
    {
        transform.position += travelDirection * bulletSpeed * deltaTime;
    }
}
