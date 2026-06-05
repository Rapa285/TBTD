using UnityEngine;

/// <summary>
/// Scene-level bridge from generic tower lifecycle events to shared tower SFX.
/// </summary>
public sealed class MiscTowerSFXProvider : MonoBehaviour
{
    [SerializeField, Tooltip("Event bus used for tower lifecycle and progression notifications. Uses the scene service locator when empty.")]
    private UnitEventBus eventBus;

    [SerializeField, Tooltip("Audio manager used to play tower SFX. Uses the scene service locator when empty.")]
    private AudioManager audioManager;

    [Header("SFX Clips")]
    [SerializeField, Tooltip("Played after a tower completes deployment activation.")]
    private AudioClip deploySound;

    [SerializeField, Tooltip("Played once after a deployed tower's setup time has elapsed.")]
    private AudioClip preparingCompleteSound;

    [SerializeField, Tooltip("Played when the player starts holding recall for a deployed roster unit.")]
    private AudioClip recallingSound;

    [SerializeField, Tooltip("Played after a deployed roster unit has been successfully recalled.")]
    private AudioClip recallCompleteSound;

    [SerializeField, Tooltip("Played after an upgrade selection advances a roster unit's level.")]
    private AudioClip levelUpSound;

    [Header("Pitch Randomization")]
    [SerializeField] private bool randomizeDeployPitch = true;
    [SerializeField] private bool randomizePreparingCompletePitch = true;
    [SerializeField] private bool randomizeRecallingPitch = true;
    [SerializeField] private bool randomizeRecallCompletePitch = true;
    [SerializeField] private bool randomizeLevelUpPitch = true;

    private bool subscribedToEventBus;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        SubscribeToEventBus();
    }

    private void Start()
    {
        SubscribeToEventBus();
    }

    private void Update()
    {
        if (!subscribedToEventBus)
        {
            SubscribeToEventBus();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromEventBus();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEventBus();
    }

    private void HandleTowerDeployed(TowerDeployedEvent eventData)
    {
        PlayTowerSound(deploySound, randomizeDeployPitch);
    }

    private void HandleTowerSetupCompleted(TowerSetupCompletedEvent eventData)
    {
        PlayTowerSound(preparingCompleteSound, randomizePreparingCompletePitch);
    }

    private void HandleUnitRecallStarted(UnitRecallStartedEvent eventData)
    {
        PlayTowerSound(recallingSound, randomizeRecallingPitch);
    }

    private void HandleUnitRecalled(UnitRecalledEvent eventData)
    {
        PlayTowerSound(recallCompleteSound, randomizeRecallCompletePitch);
    }

    private void HandleUnitUpgradeSelected(UnitUpgradeSelectedEvent eventData)
    {
        PlayTowerSound(levelUpSound, randomizeLevelUpPitch);
    }

    private void PlayTowerSound(AudioClip clip, bool randomizePitch)
    {
        if (clip == null)
        {
            return;
        }

        AudioManager resolvedAudioManager = ResolveAudioManager();
        if (resolvedAudioManager != null)
        {
            resolvedAudioManager.PlayTowerSFX(clip, randomizePitch);
        }
    }

    private void ResolveReferences()
    {
        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }

        ResolveAudioManager();
    }

    private AudioManager ResolveAudioManager()
    {
        if (audioManager != null)
        {
            return audioManager;
        }

        if (!ServiceLocator.TryResolve(out audioManager))
        {
            audioManager = AudioManager.Instance;
        }

        return audioManager;
    }

    private void SubscribeToEventBus()
    {
        if (subscribedToEventBus)
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

        eventBus.TowerDeployed += HandleTowerDeployed;
        eventBus.TowerSetupCompleted += HandleTowerSetupCompleted;
        eventBus.UnitRecallStarted += HandleUnitRecallStarted;
        eventBus.UnitRecalled += HandleUnitRecalled;
        eventBus.UnitUpgradeSelected += HandleUnitUpgradeSelected;
        subscribedToEventBus = true;
    }

    private void UnsubscribeFromEventBus()
    {
        if (!subscribedToEventBus || eventBus == null)
        {
            return;
        }

        eventBus.TowerDeployed -= HandleTowerDeployed;
        eventBus.TowerSetupCompleted -= HandleTowerSetupCompleted;
        eventBus.UnitRecallStarted -= HandleUnitRecallStarted;
        eventBus.UnitRecalled -= HandleUnitRecalled;
        eventBus.UnitUpgradeSelected -= HandleUnitUpgradeSelected;
        subscribedToEventBus = false;
    }
}
