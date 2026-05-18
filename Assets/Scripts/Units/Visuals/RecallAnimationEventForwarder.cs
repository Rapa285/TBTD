using UnityEngine;

/// <summary>
/// Forwards recall animation events from child Animator objects to the parent controller.
/// </summary>
public class RecallAnimationEventForwarder : MonoBehaviour
{
    [SerializeField, Tooltip("Parent recall controller that owns the shared recall effect objects. Auto-resolved from parents when empty.")]
    private RecallAnimController recallAnimController;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void MarkRecallInProgressAnimationComplete()
    {
        ResolveReferences();
        recallAnimController?.MarkRecallInProgressAnimationComplete();
    }

    public void MarkRecallSuccessAnimationComplete()
    {
        ResolveReferences();
        recallAnimController?.MarkRecallSuccessAnimationComplete();
    }

    private void ResolveReferences()
    {
        if (recallAnimController == null)
        {
            recallAnimController = GetComponentInParent<RecallAnimController>(true);
        }
    }
}
