using UnityEngine;

/// <summary>
/// Runtime progression component for one deployed unit instance.
/// </summary>
public class UnitProgression : MonoBehaviour
{
    [SerializeField, Tooltip("Event bus used to publish XP and upgrade-threshold changes.")]
    private UnitEventBus eventBus;

    [SerializeField, Tooltip("Roster unit ID this runtime progression belongs to.")]
    private string unitId;

    [SerializeField, Tooltip("Current roster level for this deployed unit.")]
    private int level = 1;

    [SerializeField, Tooltip("Current accumulated experience for this unit.")]
    private float currentExperience;

    [SerializeField, Tooltip("Whether this unit has another configured upgrade threshold.")]
    private bool hasNextExperienceThreshold;

    [SerializeField, Tooltip("Experience required to trigger the next upgrade offer.")]
    private float nextExperienceThreshold;

    [SerializeField, Tooltip("Whether this unit is waiting for an upgrade selection.")]
    private bool upgradePending;

    public string UnitId => unitId;
    public int Level => Mathf.Max(1, level);
    public float CurrentExperience => Mathf.Max(0f, currentExperience);
    public bool HasNextExperienceThreshold => hasNextExperienceThreshold;
    public float NextExperienceThreshold => nextExperienceThreshold;
    public bool UpgradePending => upgradePending;

    /// <summary>
    /// Rehydrates runtime progression from the persistent roster state.
    /// </summary>
    public void Initialize(
        string unitId,
        int level,
        float currentExperience,
        float nextExperienceThreshold,
        bool hasNextExperienceThreshold,
        bool upgradePending,
        UnitEventBus eventBus,
        bool evaluateThreshold = true)
    {
        this.unitId = unitId;
        this.level = Mathf.Max(1, level);
        this.currentExperience = Mathf.Max(0f, currentExperience);
        this.nextExperienceThreshold = Mathf.Max(0f, nextExperienceThreshold);
        this.hasNextExperienceThreshold = hasNextExperienceThreshold;
        this.upgradePending = upgradePending;
        this.eventBus = eventBus;

        if (evaluateThreshold)
        {
            EvaluateThreshold();
        }
    }

    /// <summary>
    /// Adds XP, publishes the change, and checks whether this unit should request an upgrade offer.
    /// </summary>
    public void AddExperience(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        float previousExperience = currentExperience;
        currentExperience += amount;

        if (eventBus != null)
        {
            eventBus.RaiseUnitExperienceChanged(new UnitExperienceChangedEvent(
                unitId,
                previousExperience,
                currentExperience,
                amount,
                Level));
        }

        EvaluateThreshold();
    }

    private void EvaluateThreshold()
    {
        // One pending choice is resolved at a time; retained XP is evaluated again after selection.
        if (upgradePending
            || !hasNextExperienceThreshold
            || currentExperience < nextExperienceThreshold)
        {
            return;
        }

        upgradePending = true;

        if (eventBus != null)
        {
            eventBus.RaiseUnitUpgradeThresholdReached(new UnitUpgradeThresholdReachedEvent(
                unitId,
                currentExperience,
                nextExperienceThreshold,
                Level));
        }
    }
}
