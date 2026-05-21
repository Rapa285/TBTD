using System.Collections;
using UnityEngine;

/// <summary>
/// Disables a pooled VFX object after a scaled-time duration.
/// </summary>
public sealed class VFXSelfDisable : MonoBehaviour
{
    [SerializeField, Min(0f), Tooltip("Scaled seconds before this VFX object is returned to the inactive pool.")]
    private float disableAfterSeconds = 1f;

    private Coroutine disableRoutine;

    private void OnEnable()
    {
        if (disableRoutine != null)
        {
            StopCoroutine(disableRoutine);
        }

        disableRoutine = StartCoroutine(DisableAfterDelay());
    }

    private void OnDisable()
    {
        if (disableRoutine != null)
        {
            StopCoroutine(disableRoutine);
            disableRoutine = null;
        }
    }

    private void OnValidate()
    {
        disableAfterSeconds = Mathf.Max(0f, disableAfterSeconds);
    }

    private IEnumerator DisableAfterDelay()
    {
        yield return new WaitForSeconds(disableAfterSeconds);
        disableRoutine = null;
        gameObject.SetActive(false);
    }
}
