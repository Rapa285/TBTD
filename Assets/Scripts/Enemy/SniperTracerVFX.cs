using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class SniperTracerVFX : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lingerDuration = 0.2f;
    [SerializeField] private AnimationCurve alphaFadeCurve;

    private LineRenderer line;
    private Coroutine fadeRoutine;
    private Color originalStartColor;
    private Color originalEndColor;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        originalStartColor = line.startColor;
        originalEndColor = line.endColor;
        
        if (alphaFadeCurve.length == 0)
        {
            alphaFadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        }
    }

    public void SetupTracer(Vector3 startPos, Vector3 endPos)
    {
        gameObject.SetActive(true);
        
        line.positionCount = 2;
        line.SetPosition(0, startPos);
        line.SetPosition(1, endPos);

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < lingerDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / lingerDuration;

            float alphaValue = alphaFadeCurve.Evaluate(normalizedTime);


            SetLineAlpha(alphaValue);

            yield return null;
        }

        gameObject.SetActive(false); 
    }

    private void SetLineAlpha(float alpha)
    {
        Color sC = originalStartColor;
        sC.a = alpha;
        line.startColor = sC;

        Color eC = originalEndColor;
        eC.a = alpha;
        line.endColor = eC;
    }
}