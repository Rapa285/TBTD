using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Events;

public class PooledObject : MonoBehaviour
{
    [HideInInspector] public UnityEvent OnGet;
    [HideInInspector] public UnityEvent OnRelease;
    private IObjectPool<PooledObject> _managedPool;

    public void SetPool(IObjectPool<PooledObject> pool)
    {
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
