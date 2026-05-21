using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene-level pooled VFX playback service.
/// </summary>
[DefaultExecutionOrder(-850)]
public sealed class VFXService : MonoBehaviour
{
    [SerializeField, Tooltip("VFX definitions this service can pool and play.")]
    private List<VFXDefSO> availableVFXs = new List<VFXDefSO>();

    [SerializeField, Min(0), Tooltip("Number of inactive instances created for each VFX definition on start.")]
    private int initialPoolSize = 4;

    private readonly Dictionary<VFXDefSO, Transform> poolRootsByDefinition = new Dictionary<VFXDefSO, Transform>();
    private readonly HashSet<VFXType> warnedMissingMappings = new HashSet<VFXType>();
    private readonly HashSet<GameObject> warnedMissingSelfDisablePrefabs = new HashSet<GameObject>();

    public IReadOnlyList<VFXDefSO> AvailableVFXs => availableVFXs;

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
        ServiceLocator.Unregister<VFXService>(this);
    }

    private void OnValidate()
    {
        initialPoolSize = Mathf.Max(0, initialPoolSize);
    }

    public void HandleRequest(VFXType vfxType, Transform targetLocation)
    {
        if (vfxType == VFXType.None || targetLocation == null)
        {
            return;
        }

        VFXDefSO vfxToPlay = ResolveDefinition(vfxType);
        if (vfxToPlay == null)
        {
            WarnMissingMapping(vfxType);
            return;
        }

        GameObject instance = GetPooledInstance(vfxToPlay);
        if (instance == null)
        {
            return;
        }

        Transform instanceTransform = instance.transform;
        instanceTransform.SetPositionAndRotation(targetLocation.position, targetLocation.rotation);
        instance.SetActive(true);
    }

    private VFXDefSO ResolveDefinition(VFXType vfxType)
    {
        for (int i = 0; i < availableVFXs.Count; i++)
        {
            VFXDefSO definition = availableVFXs[i];
            if (CanPool(definition) && definition.VFXType == vfxType)
            {
                return definition;
            }
        }

        return null;
    }

    private void ValidateDefinitions()
    {
        HashSet<VFXType> seenTypes = new HashSet<VFXType>();
        for (int i = 0; i < availableVFXs.Count; i++)
        {
            VFXDefSO definition = availableVFXs[i];
            if (definition == null)
            {
                Debug.LogWarning(
                    $"{nameof(VFXService)} on '{name}' has a null VFX definition at index {i}.",
                    this);
                continue;
            }

            if (definition.VFXPrefab == null)
            {
                Debug.LogWarning(
                    $"{nameof(VFXService)} on '{name}' has VFX definition '{definition.name}' without a prefab.",
                    this);
                continue;
            }

            if (definition.VFXType == VFXType.None)
            {
                Debug.LogWarning(
                    $"{nameof(VFXService)} on '{name}' has VFX definition '{definition.name}' using {nameof(VFXType)}.{nameof(VFXType.None)}.",
                    this);
                continue;
            }

            if (!seenTypes.Add(definition.VFXType))
            {
                Debug.LogWarning(
                    $"{nameof(VFXService)} on '{name}' has multiple valid definitions for {definition.VFXType}. The first valid entry will be used.",
                    this);
            }
        }
    }

    private void BuildInitialPools()
    {
        for (int i = 0; i < availableVFXs.Count; i++)
        {
            VFXDefSO definition = availableVFXs[i];
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

    private GameObject GetPooledInstance(VFXDefSO definition)
    {
        if (!CanPool(definition))
        {
            return null;
        }

        Transform poolRoot = GetOrCreatePoolRoot(definition);
        for (int i = 0; i < poolRoot.childCount; i++)
        {
            GameObject child = poolRoot.GetChild(i).gameObject;
            if (!child.activeSelf)
            {
                return child;
            }
        }

        return CreatePooledInstance(definition, poolRoot);
    }

    private GameObject CreatePooledInstance(VFXDefSO definition, Transform poolRoot)
    {
        GameObject instance = Instantiate(definition.VFXPrefab, poolRoot);
        instance.name = $"{definition.VFXPrefab.name} (VFX Pool)";
        EnsureSelfDisable(definition, instance);
        instance.SetActive(false);
        return instance;
    }

    private void EnsureSelfDisable(VFXDefSO definition, GameObject instance)
    {
        if (instance == null || instance.GetComponent<VFXSelfDisable>() != null)
        {
            return;
        }

        if (definition != null
            && definition.VFXPrefab != null
            && warnedMissingSelfDisablePrefabs.Add(definition.VFXPrefab))
        {
            Debug.LogWarning(
                $"{nameof(VFXService)} auto-added {nameof(VFXSelfDisable)} to pooled instances of '{definition.VFXPrefab.name}' because the prefab does not include one.",
                this);
        }

        instance.AddComponent<VFXSelfDisable>();
    }

    private Transform GetOrCreatePoolRoot(VFXDefSO definition)
    {
        if (poolRootsByDefinition.TryGetValue(definition, out Transform existingRoot)
            && existingRoot != null)
        {
            return existingRoot;
        }

        GameObject rootObject = new GameObject($"{definition.name} VFX Pool");
        Transform root = rootObject.transform;
        root.SetParent(transform, false);
        poolRootsByDefinition[definition] = root;
        return root;
    }

    private bool CanPool(VFXDefSO definition)
    {
        return definition != null && definition.VFXPrefab != null;
    }

    private void WarnMissingMapping(VFXType vfxType)
    {
        if (!warnedMissingMappings.Add(vfxType))
        {
            return;
        }

        Debug.LogWarning(
            $"{nameof(VFXService)} on '{name}' has no valid VFX definition for {vfxType}.",
            this);
    }

    private void RegisterWithServiceLocator()
    {
        if (ServiceLocator.TryResolve<VFXService>(out VFXService existingService)
            && existingService != null
            && existingService != this)
        {
            Debug.LogWarning(
                $"{nameof(VFXService)} on '{name}' replaced the previously registered {nameof(VFXService)} on '{existingService.name}'.",
                this);
        }

        ServiceLocator.Register<VFXService>(this);
    }
}
