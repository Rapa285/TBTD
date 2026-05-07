using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    [SerializeField] private HealthComponent healthComponent;
    [SerializeField] private Image hpBarFill;
    [SerializeField] private Image shieldBarFill;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera=Camera.main;
        if (healthComponent == null)
        {
            healthComponent=GetComponentInParent<HealthComponent>();
        }
    }

    private void Update()
    {
        if (healthComponent != null)
        {
            if (hpBarFill != null)
            {
                hpBarFill.fillAmount=healthComponent.CurrentHealth/healthComponent.MaxHealth;
            }
            if (shieldBarFill != null)
            {
                // assume max shield is 100
                shieldBarFill.fillAmount=healthComponent.CurrentShield/100f;
                shieldBarFill.gameObject.SetActive(healthComponent.CurrentShield>0);
            }
        }
    }

    private void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.LookAt(transform.position+mainCamera.transform.rotation*Vector3.forward,mainCamera.transform.rotation*Vector3.up);
        }
    }
}