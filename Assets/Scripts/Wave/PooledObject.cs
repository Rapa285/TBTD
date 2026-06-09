using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Events;

public class PooledObject : MonoBehaviour
{
    [HideInInspector] public UnityEvent OnGet = new UnityEvent();
    [HideInInspector] public UnityEvent OnRelease = new UnityEvent();
    private IObjectPool<PooledObject> _managedPool;

    private void Awake()
    {
        EnsureEvents();
    }

    public void EnsureEvents()
    {
        if (OnGet == null)
        {
            OnGet = new UnityEvent();
        }

        if (OnRelease == null)
        {
            OnRelease = new UnityEvent();
        }
    }

    public void SetPool(IObjectPool<PooledObject> pool)
    {
        EnsureEvents();
        _managedPool = pool;
    }

    public void ReturnToPool()
    {
        if (_managedPool != null)
        {
            _managedPool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
