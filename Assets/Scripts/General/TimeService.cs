using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene-level authority for gameplay time scale. Pause requests always override slowdown requests.
/// </summary>
[DefaultExecutionOrder(-950)]
public class TimeService : MonoBehaviour
{
    private const string DeploymentPreviewKey = "DeploymentPreview";
    private const string UpgradeMenuKey = "UpgradeMenu";
    private const string GeneralPauseKey = "GeneralPause";
    private const string GameOverKey = "GameOver";
    private const string SlowDownEventKey = "SlowDownGameEvent";

    private static TimeService instance;

    [SerializeField, Min(0f), Tooltip("Normal gameplay time scale when no pause or slowdown request is active.")]
    private float normalTimeScale = 1f;

    [SerializeField, Range(0.01f, 1f), Tooltip("Time scale applied while a tower deployment preview is being dragged.")]
    private float deploymentPreviewTimeScale = 0.2f;

    [SerializeField, Tooltip("Scale fixedDeltaTime with timeScale so physics keeps the same step density during slow motion.")]
    private bool scaleFixedDeltaTime = true;

    private readonly HashSet<object> pauseRequests = new HashSet<object>();
    private readonly HashSet<object> playerPauseRequests = new HashSet<object>();
    private readonly Dictionary<object, float> slowdownRequests = new Dictionary<object, float>();

    private UnitEventBus eventBus;
    private UpgradesManager upgradesManager;
    private float baseFixedDeltaTime;
    private float currentTimeScale = 1f;
    private bool unitEventBusSubscribed;
    private bool generalEventBusSubscribed;

    public static TimeService Instance
    {
        get
        {
            EnsureInstance();
            return instance;
        }
    }

    public float CurrentTimeScale => currentTimeScale;
    public bool IsPaused => pauseRequests.Count > 0;
    public bool IsSlowed => pauseRequests.Count == 0 && slowdownRequests.Count > 0;

    public event Action<float> TimeScaleChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstanceAfterSceneLoad()
    {
        EnsureInstance();
    }

    public static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        instance = FindAnyObjectByType<TimeService>();
        if (instance != null)
        {
            return;
        }

        GameObject serviceObject = new GameObject(nameof(TimeService));
        DontDestroyOnLoad(serviceObject);
        instance = serviceObject.AddComponent<TimeService>();
    }

    public void RequestPause(object owner, bool controlsPlayerPauseState = false)
    {
        if (owner == null)
        {
            return;
        }

        bool playerPauseChanged = controlsPlayerPauseState && playerPauseRequests.Add(owner);
        if (pauseRequests.Add(owner))
        {
            ApplyResolvedTimeScale();
        }

        if (playerPauseChanged)
        {
            RefreshPlayerPausedState();
        }
    }

    public void ReleasePause(object owner)
    {
        if (owner == null)
        {
            return;
        }

        bool pauseChanged = pauseRequests.Remove(owner);
        bool playerPauseChanged = playerPauseRequests.Remove(owner);

        if (pauseChanged)
        {
            ApplyResolvedTimeScale();
        }

        if (playerPauseChanged)
        {
            RefreshPlayerPausedState();
        }
    }

    public void RequestSlowdown(object owner, float timeScale)
    {
        if (owner == null)
        {
            return;
        }

        float clampedScale = Mathf.Clamp(timeScale, 0.01f, normalTimeScale);
        if (slowdownRequests.TryGetValue(owner, out float existingScale)
            && Mathf.Approximately(existingScale, clampedScale))
        {
            return;
        }

        slowdownRequests[owner] = clampedScale;
        ApplyResolvedTimeScale();
    }

    public void ReleaseSlowdown(object owner)
    {
        if (owner == null)
        {
            return;
        }

        if (slowdownRequests.Remove(owner))
        {
            ApplyResolvedTimeScale();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        baseFixedDeltaTime = Time.fixedDeltaTime;
        currentTimeScale = Time.timeScale;
        RegisterWithServiceLocator();
        ApplyResolvedTimeScale();
    }

    private void OnEnable()
    {
        SubscribeToGeneralEvents();
        ResolveReferences();
        SubscribeToUnitEventBus();
    }

    private void Start()
    {
        ResolveReferences();
        SubscribeToUnitEventBus();
    }

    private void Update()
    {
        if (unitEventBusSubscribed && eventBus == null)
        {
            unitEventBusSubscribed = false;
            eventBus = null;
        }

        if (!unitEventBusSubscribed)
        {
            ResolveReferences();
            SubscribeToUnitEventBus();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromUnitEventBus();
        UnsubscribeFromGeneralEvents();
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        ServiceLocator.Unregister<TimeService>(this);
        ReleaseAutomaticRequests();
        Time.timeScale = normalTimeScale;
        if (scaleFixedDeltaTime && baseFixedDeltaTime > 0f)
        {
            Time.fixedDeltaTime = baseFixedDeltaTime;
        }

        instance = null;
    }

    private void HandleGamePaused(GamePausedEvent eventData)
    {
        RequestPause(GeneralPauseKey, true);
    }

    private void HandleGameUnPaused(GameUnPausedEvent eventData)
    {
        ReleasePause(GeneralPauseKey);
    }

    private void HandleGameOver(GameOverEvent eventData)
    {
        RequestPause(GameOverKey);

        if (ServiceLocator.TryResolve(out PlayerStateController playerStateController))
        {
            playerStateController.SetGameOver(true);
        }
    }

    private void HandleSlowDownGame(SlowDownGameEvent eventData)
    {
        if (eventData.SlowDownScale < normalTimeScale)
        {
            RequestSlowdown(SlowDownEventKey, eventData.SlowDownScale);
        }
        else
        {
            ReleaseSlowdown(SlowDownEventKey);
        }
    }

    private void HandleDeploymentPreviewStarted(UnitDeploymentPreviewStartedEvent eventData)
    {
        RequestSlowdown(DeploymentPreviewKey, deploymentPreviewTimeScale);
    }

    private void HandleDeploymentPreviewEnded(UnitDeploymentPreviewEndedEvent eventData)
    {
        ReleaseSlowdown(DeploymentPreviewKey);
    }

    private void HandleUpgradeChoicesOffered(UnitUpgradeChoicesOfferedEvent eventData)
    {
        if (!string.IsNullOrWhiteSpace(eventData.UnitId))
        {
            RequestPause(UpgradeMenuKey);
        }
    }

    private void HandleUpgradeSelected(UnitUpgradeSelectedEvent eventData)
    {
        if (upgradesManager == null)
        {
            ResolveReferences();
        }

        if (upgradesManager != null && upgradesManager.HasPendingOffer(eventData.UnitId))
        {
            return;
        }

        ReleasePause(UpgradeMenuKey);
    }

    private void HandleUpgradeMenuClosed(UnitUpgradeMenuClosedEvent eventData)
    {
        ReleasePause(UpgradeMenuKey);
    }

    private void ApplyResolvedTimeScale()
    {
        float resolvedTimeScale = ResolveTimeScale();
        if (Mathf.Approximately(currentTimeScale, resolvedTimeScale)
            && Mathf.Approximately(Time.timeScale, resolvedTimeScale))
        {
            return;
        }

        currentTimeScale = resolvedTimeScale;
        Time.timeScale = resolvedTimeScale;

        if (scaleFixedDeltaTime && baseFixedDeltaTime > 0f)
        {
            Time.fixedDeltaTime = baseFixedDeltaTime * Mathf.Max(resolvedTimeScale, 0.0001f);
        }

        TimeScaleChanged?.Invoke(currentTimeScale);
    }

    private float ResolveTimeScale()
    {
        if (pauseRequests.Count > 0)
        {
            return 0f;
        }

        float resolvedTimeScale = Mathf.Max(0f, normalTimeScale);
        foreach (float requestedScale in slowdownRequests.Values)
        {
            resolvedTimeScale = Mathf.Min(resolvedTimeScale, requestedScale);
        }

        return resolvedTimeScale;
    }

    private void ReleaseAutomaticRequests()
    {
        ReleaseSlowdown(DeploymentPreviewKey);
        ReleaseSlowdown(SlowDownEventKey);
        ReleasePause(UpgradeMenuKey);
        ReleasePause(GeneralPauseKey);
        ReleasePause(GameOverKey);
    }

    private void RefreshPlayerPausedState()
    {
        if (ServiceLocator.TryResolve(out PlayerStateController playerStateController))
        {
            playerStateController.SetPaused(playerPauseRequests.Count > 0);
        }
    }

    private void ResolveReferences()
    {
        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }

        if (upgradesManager == null)
        {
            ServiceLocator.TryResolve(out upgradesManager);
        }
    }

    private void SubscribeToUnitEventBus()
    {
        if (unitEventBusSubscribed)
        {
            return;
        }

        if (eventBus == null)
        {
            ResolveReferences();
        }

        if (eventBus == null)
        {
            return;
        }

        eventBus.UnitDeploymentPreviewStarted += HandleDeploymentPreviewStarted;
        eventBus.UnitDeploymentPreviewEnded += HandleDeploymentPreviewEnded;
        eventBus.UnitUpgradeChoicesOffered += HandleUpgradeChoicesOffered;
        eventBus.UnitUpgradeSelected += HandleUpgradeSelected;
        eventBus.UnitUpgradeMenuClosed += HandleUpgradeMenuClosed;
        unitEventBusSubscribed = true;
    }

    private void UnsubscribeFromUnitEventBus()
    {
        if (!unitEventBusSubscribed || eventBus == null)
        {
            unitEventBusSubscribed = false;
            return;
        }

        eventBus.UnitDeploymentPreviewStarted -= HandleDeploymentPreviewStarted;
        eventBus.UnitDeploymentPreviewEnded -= HandleDeploymentPreviewEnded;
        eventBus.UnitUpgradeChoicesOffered -= HandleUpgradeChoicesOffered;
        eventBus.UnitUpgradeSelected -= HandleUpgradeSelected;
        eventBus.UnitUpgradeMenuClosed -= HandleUpgradeMenuClosed;
        unitEventBusSubscribed = false;
    }

    private void SubscribeToGeneralEvents()
    {
        if (generalEventBusSubscribed)
        {
            return;
        }

        GeneralEventBus<GamePausedEvent>.Subscribe(HandleGamePaused);
        GeneralEventBus<GameUnPausedEvent>.Subscribe(HandleGameUnPaused);
        GeneralEventBus<GameOverEvent>.Subscribe(HandleGameOver);
        GeneralEventBus<SlowDownGameEvent>.Subscribe(HandleSlowDownGame);
        generalEventBusSubscribed = true;
    }

    private void UnsubscribeFromGeneralEvents()
    {
        if (!generalEventBusSubscribed)
        {
            return;
        }

        GeneralEventBus<GamePausedEvent>.Unsubscribe(HandleGamePaused);
        GeneralEventBus<GameUnPausedEvent>.Unsubscribe(HandleGameUnPaused);
        GeneralEventBus<GameOverEvent>.Unsubscribe(HandleGameOver);
        GeneralEventBus<SlowDownGameEvent>.Unsubscribe(HandleSlowDownGame);
        generalEventBusSubscribed = false;
    }

    private void RegisterWithServiceLocator()
    {
        if (ServiceLocator.TryResolve<TimeService>(out TimeService existingTimeService)
            && existingTimeService != null
            && existingTimeService != this)
        {
            Debug.LogWarning(
                $"{nameof(TimeService)} on '{name}' replaced the previously registered {nameof(TimeService)} on '{existingTimeService.name}'.",
                this);
        }

        ServiceLocator.Register<TimeService>(this);
    }
}
