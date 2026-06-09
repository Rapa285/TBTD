using UnityEngine;
using TMPro;
using System.Collections;

public class DamagePopupVFX : MonoBehaviour
{
    public static float GlobalPendingDamage = 0f; 

    [SerializeField] private TextMeshPro textMesh;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float disappearTimer = 1f;

    private Color textColor;
    private Coroutine animateRoutine;
    
    private Transform camTransform; 

    private void Awake()
    {
        if (textMesh == null) textMesh = GetComponent<TextMeshPro>();
        
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
    }

    private void OnEnable()
    {
        Setup(GlobalPendingDamage);
    }

    private void LateUpdate()
    {
        if (camTransform != null)
        {
            transform.forward = camTransform.forward;
        }
    }

    private void Setup(float damageAmount)
    {
        textMesh.text = damageAmount.ToString("0");
        textColor = textMesh.color;
        textColor.a = 1f;
        textMesh.color = textColor;

        transform.rotation = Quaternion.identity;

        transform.position += (Vector3.up * 1.5f) + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);

        if (animateRoutine != null) StopCoroutine(animateRoutine);
        animateRoutine = StartCoroutine(AnimatePopup());
    }

    private IEnumerator AnimatePopup()
    {
        float timer = 0f;
        while (timer < disappearTimer)
        {
            timer += Time.deltaTime;
            transform.position += Vector3.up * floatSpeed * Time.deltaTime;

            if (timer > disappearTimer / 2f)
            {
                float fadeRatio = 1f - ((timer - (disappearTimer / 2f)) / (disappearTimer / 2f));
                textColor.a = fadeRatio;
                textMesh.color = textColor;
            }
            yield return null;
        }

        gameObject.SetActive(false); 
    }
}