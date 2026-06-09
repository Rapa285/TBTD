using UnityEngine;

/// <summary>
/// Runtime progression component for one deployed unit instance.
/// </summary>
public class UnitProgression : MonoBehaviour
{
    [SerializeField, Tooltip("Event bus used to publish XP changes.")]
    private UnitEventBus eventBus;

    [SerializeField, Tooltip("Roster unit ID this runtime progression belongs to.")]
    private string unitId;

    [SerializeField, Min(0), Tooltip("Current roster level for this deployed unit.")]
    private int level;

    [SerializeField, Tooltip("Current accumulated experience for this unit.")]
    private float currentExperience;

    [SerializeField, Tooltip("Whether this unit has another configured upgrade threshold.")]
    private bool hasNextExperienceThreshold;

    [SerializeField, Tooltip("Experience required for the next level-up.")]
    private float nextExperienceThreshold;

    public string UnitId => unitId;
    public int Level => Mathf.Max(0, level);
    public float CurrentExperience => Mathf.Max(0f, currentExperience);
    public bool HasNextExperienceThreshold => hasNextExperienceThreshold;
    public float NextExperienceThreshold => nextExperienceThreshold;

    public void AssignRuntimeUnitId(string unitId)
    {
        this.unitId = unitId ?? string.Empty;

        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }
    }

    /// <summary>
    /// Rehydrates runtime progression from the persistent roster state.
    /// </summary>
    public void Initialize(
        string unitId,
        int level,
        float currentExperience,
        float nextExperienceThreshold,
        bool hasNextExperienceThreshold,
        UnitEventBus eventBus)
    {
        this.unitId = unitId;
        this.level = Mathf.Max(0, level);
        this.currentExperience = Mathf.Max(0f, currentExperience);
        this.nextExperienceThreshold = Mathf.Max(0f, nextExperienceThreshold);
        this.hasNextExperienceThreshold = hasNextExperienceThreshold;
        this.eventBus = eventBus;
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

    }
}
