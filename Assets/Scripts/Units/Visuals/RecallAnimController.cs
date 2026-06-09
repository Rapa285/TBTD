using UnityEngine;

/// <summary>
/// Scene-level coordinator for recall progress and success world effects.
/// </summary>
public class RecallAnimController : MonoBehaviour
{
    private const float AuthoredAnimationDuration = 1f;

    [SerializeField, Tooltip("World object shown at the tower while recall is being held.")]
    private GameObject recallInProgress;

    [SerializeField, Tooltip("World object shown at the tower after recall completes.")]
    private GameObject recallSuccess;

    [SerializeField, Tooltip("Optional animator used by the in-progress recall effect.")]
    private Animator recallInProgressAnimator;

    [SerializeField, Tooltip("Optional animator used by the successful recall effect.")]
    private Animator recallSuccessAnimator;

    [SerializeField, Tooltip("Animator float parameter used as a speed multiplier so a one second source animation lasts for the recall duration.")]
    private string durationMultiplierParameter = "DurationMult";

    [SerializeField, Tooltip("World-space offset applied to the tower position before placing recall effects.")]
    private Vector3 worldOffset;

    private void Awake()
    {
        ResolveReferences();
        RegisterWithServiceLocator();
        HideRecallInProgress();
        HideRecallSuccess();
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister(this);
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void PlayRecallInProgress(TowerEntity tower)
    {
        PlayRecallInProgress(tower, 1f);
    }

    public void PlayRecallInProgress(TowerEntity tower, float recallDuration)
    {
        if (tower == null)
        {
            return;
        }

        PlayRecallInProgressAtPosition(tower.transform.position, recallDuration);
    }

    public void CancelRecallInProgress(TowerEntity tower)
    {
        HideRecallInProgress();
    }

    public void PlayRecallSuccess(TowerEntity tower)
    {
        PlayRecallSuccess(tower, 1f);
    }

    public void PlayRecallSuccess(TowerEntity tower, float recallDuration)
    {
        if (tower == null)
        {
            return;
        }

        PlayRecallSuccessAtPosition(tower.transform.position, recallDuration);
    }

    public void PlayRecallSuccessAtPosition(Vector3 worldPosition)
    {
        PlayRecallSuccessAtPosition(worldPosition, 1f);
    }

    public void PlayRecallSuccessAtPosition(Vector3 worldPosition, float recallDuration)
    {
        PlayEffect(recallSuccess, recallSuccessAnimator, worldPosition, recallDuration);
    }

    public void HideRecallInProgress()
    {
        SetActive(recallInProgress, false);
    }

    public void HideRecallSuccess()
    {
        SetActive(recallSuccess, false);
    }

    public void MarkRecallInProgressAnimationComplete()
    {
        HideRecallInProgress();
    }

    public void MarkRecallSuccessAnimationComplete()
    {
        HideRecallSuccess();
    }

    private void PlayRecallInProgressAtPosition(Vector3 worldPosition)
    {
        PlayRecallInProgressAtPosition(worldPosition, 1f);
    }

    private void PlayRecallInProgressAtPosition(Vector3 worldPosition, float recallDuration)
    {
        PlayEffect(recallInProgress, recallInProgressAnimator, worldPosition, recallDuration);
    }

    private void PlayEffect(
        GameObject effectObject,
        Animator animator,
        Vector3 worldPosition,
        float recallDuration)
    {
        if (effectObject == null)
        {
            return;
        }

        SetActive(effectObject, false);
        effectObject.transform.position = worldPosition + worldOffset;

        if (animator != null)
        {
            SetDurationMultiplier(animator, recallDuration);
            animator.keepAnimatorStateOnDisable = false;
        }

        SetActive(effectObject, true);
    }

    private void SetDurationMultiplier(Animator animator, float recallDuration)
    {
        if (animator == null || string.IsNullOrWhiteSpace(durationMultiplierParameter))
        {
            return;
        }

        float durationMultiplier = recallDuration > 0f
            ? AuthoredAnimationDuration / recallDuration
            : 1f;

        animator.SetFloat(durationMultiplierParameter, durationMultiplier);
    }

    private void ResolveReferences()
    {
        if (recallInProgressAnimator == null && recallInProgress != null)
        {
            recallInProgressAnimator = recallInProgress.GetComponentInChildren<Animator>(true);
        }

        if (recallSuccessAnimator == null && recallSuccess != null)
        {
            recallSuccessAnimator = recallSuccess.GetComponentInChildren<Animator>(true);
        }
    }

    private void RegisterWithServiceLocator()
    {
        if (ServiceLocator.TryResolve(out RecallAnimController existingController)
            && existingController != null
            && existingController != this)
        {
            Debug.LogWarning(
                $"{nameof(RecallAnimController)} on '{name}' replaced the previously registered {nameof(RecallAnimController)} on '{existingController.name}'.",
                this);
        }

        ServiceLocator.Register(this);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
