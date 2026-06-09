using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene-level pooled projectile service.
/// </summary>
[DefaultExecutionOrder(-840)]
public sealed class ProjectilePoolService : MonoBehaviour
{
    [SerializeField, Tooltip("Projectile definitions this service can pool and provide.")]
    private List<ProjectileDefSO> availableProjectiles = new List<ProjectileDefSO>();

    [SerializeField, Min(0), Tooltip("Number of inactive instances created for each projectile definition on start.")]
    private int initialPoolSize = 8;

    private readonly Dictionary<ProjectileDefSO, Transform> poolRootsByDefinition = new Dictionary<ProjectileDefSO, Transform>();
    private readonly HashSet<ProjectileType> warnedMissingMappings = new HashSet<ProjectileType>();
    private readonly HashSet<ProjectileType> warnedTypeMismatches = new HashSet<ProjectileType>();

    public IReadOnlyList<ProjectileDefSO> AvailableProjectiles => availableProjectiles;

    private void Awake()
    {
        RegisterWithServiceLocator();
    }

    private void Start()
    {
        ValidateDefinitions();
        BuildInitialPools();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<ProjectilePoolService>(this);
    }

    private void OnValidate()
    {
        initialPoolSize = Mathf.Max(0, initialPoolSize);
    }

    public bool TryRequestProjectile<T>(
        ProjectileType projectileType,
        Vector3 position,
        Quaternion rotation,
        out T projectile)
        where T : BaseProjectile
    {
        projectile = null;

        BaseProjectile baseProjectile = GetPooledProjectile(projectileType);
        if (baseProjectile == null)
        {
            return false;
        }

        T typedProjectile = baseProjectile as T;
        if (typedProjectile == null)
        {
            WarnTypeMismatch<T>(projectileType, baseProjectile);
            return false;
        }

        Transform projectileTransform = typedProjectile.transform;
        projectileTransform.SetPositionAndRotation(position, rotation);
        typedProjectile.PrepareForPooledUse(this);
        typedProjectile.gameObject.SetActive(true);
        projectile = typedProjectile;
        return true;
    }

    internal void ReturnProjectile(BaseProjectile projectile)
    {
        if (projectile == null)
        {
            return;
        }

        ProjectileDefSO definition = ResolveDefinition(projectile.PooledProjectileType);
        if (definition != null)
        {
            projectile.transform.SetParent(GetOrCreatePoolRoot(definition), true);
        }

        projectile.gameObject.SetActive(false);
    }

    private BaseProjectile GetPooledProjectile(ProjectileType projectileType)
    {
        if (projectileType == ProjectileType.None)
        {
            return null;
        }

        ProjectileDefSO definition = ResolveDefinition(projectileType);
        if (definition == null)
        {
            WarnMissingMapping(projectileType);
            return null;
        }

        Transform poolRoot = GetOrCreatePoolRoot(definition);
        for (int i = 0; i < poolRoot.childCount; i++)
        {
            BaseProjectile childProjectile = poolRoot.GetChild(i).GetComponent<BaseProjectile>();
            if (childProjectile != null && !childProjectile.gameObject.activeSelf)
            {
                return childProjectile;
            }
        }

        return CreatePooledInstance(definition, poolRoot);
    }

    private ProjectileDefSO ResolveDefinition(ProjectileType projectileType)
    {
        for (int i = 0; i < availableProjectiles.Count; i++)
        {
            ProjectileDefSO definition = availableProjectiles[i];
            if (CanPool(definition) && definition.ProjectileType == projectileType)
            {
                return definition;
            }
        }

        return null;
    }

    private void ValidateDefinitions()
    {
        HashSet<ProjectileType> seenTypes = new HashSet<ProjectileType>();
        for (int i = 0; i < availableProjectiles.Count; i++)
        {
            ProjectileDefSO definition = availableProjectiles[i];
            if (definition == null)
            {
                Debug.LogWarning(
                    $"{nameof(ProjectilePoolService)} on '{name}' has a null projectile definition at index {i}.",
                    this);
                continue;
            }

            if (definition.ProjectilePrefab == null)
            {
                Debug.LogWarning(
                    $"{nameof(ProjectilePoolService)} on '{name}' has projectile definition '{definition.name}' without a prefab.",
                    this);
                continue;
            }

            if (definition.ProjectileType == ProjectileType.None)
            {
                Debug.LogWarning(
                    $"{nameof(ProjectilePoolService)} on '{name}' has projectile definition '{definition.name}' using {nameof(ProjectileType)}.{nameof(ProjectileType.None)}.",
                    this);
                continue;
            }

            if (!seenTypes.Add(definition.ProjectileType))
            {
                Debug.LogWarning(
                    $"{nameof(ProjectilePoolService)} on '{name}' has multiple valid definitions for {definition.ProjectileType}. The first valid entry will be used.",
                    this);
            }
        }
    }

    private void BuildInitialPools()
    {
        for (int i = 0; i < availableProjectiles.Count; i++)
        {
            ProjectileDefSO definition = availableProjectiles[i];
            if (!CanPool(definition))
            {
                continue;
            }

            Transform poolRoot = GetOrCreatePoolRoot(definition);
            for (int instanceIndex = poolRoot.childCount; instanceIndex < initialPoolSize; instanceIndex++)
            {
                CreatePooledInstance(definition, poolRoot);
            }
        }
    }

    private BaseProjectile CreatePooledInstance(ProjectileDefSO definition, Transform poolRoot)
    {
        BaseProjectile instance = Instantiate(definition.ProjectilePrefab, poolRoot);
        instance.name = $"{definition.ProjectilePrefab.name} (Projectile Pool)";
        instance.ConfigurePoolOwnership(this, definition.ProjectileType);
        instance.PrepareForPooledUse(this);
        instance.gameObject.SetActive(false);
        return instance;
    }

    private Transform GetOrCreatePoolRoot(ProjectileDefSO definition)
    {
        if (poolRootsByDefinition.TryGetValue(definition, out Transform existingRoot)
            && existingRoot != null)
        {
            return existingRoot;
        }

        GameObject rootObject = new GameObject($"{definition.name} Projectile Pool");
        Transform root = rootObject.transform;
        root.SetParent(transform, false);
        poolRootsByDefinition[definition] = root;
        return root;
    }

    private bool CanPool(ProjectileDefSO definition)
    {
        return definition != null && definition.ProjectilePrefab != null;
    }

    private void WarnMissingMapping(ProjectileType projectileType)
    {
        if (!warnedMissingMappings.Add(projectileType))
        {
            return;
        }

        Debug.LogWarning(
            $"{nameof(ProjectilePoolService)} on '{name}' has no valid projectile definition for {projectileType}.",
            this);
    }

    private void WarnTypeMismatch<T>(ProjectileType projectileType, BaseProjectile projectile)
        where T : BaseProjectile
    {
        if (!warnedTypeMismatches.Add(projectileType))
        {
            return;
        }

        Debug.LogWarning(
            $"{nameof(ProjectilePoolService)} resolved {projectileType} to '{projectile.name}', but the requester expected {typeof(T).Name}.",
            this);
    }

    private void RegisterWithServiceLocator()
    {
        if (ServiceLocator.TryResolve<ProjectilePoolService>(out ProjectilePoolService existingService)
            && existingService != null
            && existingService != this)
        {
            Debug.LogWarning(
                $"{nameof(ProjectilePoolService)} on '{name}' replaced the previously registered {nameof(ProjectilePoolService)} on '{existingService.name}'.",
                this);
        }

        ServiceLocator.Register<ProjectilePoolService>(this);
    }
}
