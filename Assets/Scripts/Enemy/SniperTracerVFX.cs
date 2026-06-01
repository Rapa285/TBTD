using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class SniperTracerVFX : MonoBehaviour
{
    public static Vector3 GlobalStartPos;
    public static Vector3 GlobalEndPos;

    [SerializeField] private float lingerDuration = 0.2f; 
    [SerializeField] private AnimationCurve alphaFadeCurve; 

    private LineRenderer line;
    private Color originalStartColor;
    private Color originalEndColor;
    private Coroutine fadeRoutine;

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

    private void OnEnable()
    {
        line.positionCount = 2;
        line.SetPosition(0, GlobalStartPos);
        line.SetPosition(1, GlobalEndPos);

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

            Color sC = originalStartColor; sC.a = alphaValue; line.startColor = sC;
            Color eC = originalEndColor;   eC.a = alphaValue; line.endColor = eC;

            yield return null;
        }

        gameObject.SetActive(false); 
    }
}