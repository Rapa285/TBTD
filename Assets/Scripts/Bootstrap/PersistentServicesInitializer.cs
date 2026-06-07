using UnityEngine;

[DefaultExecutionOrder(-950)]
public sealed class PersistentServicesInitializer : MonoBehaviour
{
    [Header("Initialization")]
    [SerializeField] private bool initializeOnAwake = true;
    [SerializeField] private bool ensurePersistentEventBus = true;
    [SerializeField] private bool ensureMusicService = true;
    [SerializeField] private bool ensureUISFXService = true;
    [SerializeField] private bool persistCreatedRoot = true;

    [Header("Resolved Services")]
    [SerializeField] private PersistentEventBus persistentEventBus;
    [SerializeField] private MusicService musicService;
    [SerializeField] private UISFXService uiSfxService;

    private bool initialized;
    private GameObject createdServicesRoot;

    public PersistentEventBus PersistentEventBus => persistentEventBus;
    public MusicService MusicService => musicService;
    public UISFXService UISFXService => uiSfxService;
    public bool IsInitialized => initialized;

    private void Awake()
    {
        if (initializeOnAwake)
        {
            EnsureInitialized();
        }
    }

    public void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        if (ensurePersistentEventBus)
        {
            persistentEventBus = ResolveOrCreate<PersistentEventBus>(persistentEventBus);
        }

        if (ensureMusicService)
        {
            if (ensurePersistentEventBus && persistentEventBus == null)
            {
                persistentEventBus = ResolveOrCreate<PersistentEventBus>(persistentEventBus);
            }

            musicService = ResolveOrCreate<MusicService>(musicService);
        }

        if (ensureUISFXService)
        {
            if (ensurePersistentEventBus && persistentEventBus == null)
            {
                persistentEventBus = ResolveOrCreate<PersistentEventBus>(persistentEventBus);
            }

            uiSfxService = ResolveOrCreate<UISFXService>(uiSfxService);
        }

        RefreshResolvedServiceConfig();
        initialized = true;
    }

    public void RefreshResolvedServiceConfig()
    {
        if (ConfigWorker.Instance == null)
        {
            return;
        }

        if (musicService != null)
        {
            musicService.RefreshConfig();
        }

        if (uiSfxService != null)
        {
            uiSfxService.RefreshConfig();
        }
    }

    private T ResolveOrCreate<T>(T assignedService)
        where T : Component
    {
        if (assignedService != null)
        {
            return assignedService;
        }

        if (ServiceLocator.TryResolve(out T locatedService) && locatedService != null)
        {
            return locatedService;
        }

        T sceneService = FindAnyObjectByType<T>(FindObjectsInactive.Include);
        if (sceneService != null)
        {
            return sceneService;
        }

        return GetCreatedServicesRoot().AddComponent<T>();
    }

    private GameObject GetCreatedServicesRoot()
    {
        if (createdServicesRoot != null)
        {
            return createdServicesRoot;
        }

        createdServicesRoot = new GameObject("PersistentServices");
        if (persistCreatedRoot)
        {
            DontDestroyOnLoad(createdServicesRoot);
        }

        return createdServicesRoot;
    }
}
