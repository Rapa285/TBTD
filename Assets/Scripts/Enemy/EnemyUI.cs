using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    [SerializeField] private HealthComponent healthComponent;
    [SerializeField] private Image hpBarFill;
    [SerializeField] private Image shieldBarFill;
    
    [SerializeField] private StatusEffectManager statusManager;
    [SerializeField] private Transform statusContainer;
    [SerializeField] private GameObject statusPrefab;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera=Camera.main;
        if (healthComponent == null)
        {
            healthComponent=GetComponentInParent<HealthComponent>();
        }
        if (statusManager == null)
        {
            statusManager=GetComponentInParent<StatusEffectManager>();
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
                if (healthComponent.CurrentShield > 0)
                {
                    shieldBarFill.gameObject.SetActive(true);
                    shieldBarFill.fillAmount=healthComponent.CurrentShield/healthComponent.MaxShield;
                }
                else
                {
                    shieldBarFill.gameObject.SetActive(false);
                }
            }
        }
        SyncStatusEffects();
    }

    private void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.LookAt(transform.position+mainCamera.transform.rotation*Vector3.forward,mainCamera.transform.rotation*Vector3.up);
        }
    }

    private void SyncStatusEffects()
    {
        if (statusManager==null||statusContainer==null||statusPrefab==null) return;
        var effects=statusManager.ActiveEffects;
        
        while (statusContainer.childCount<effects.Count)
        {
            Instantiate(statusPrefab,statusContainer);
        }

        while (statusContainer.childCount > effects.Count)
        {
            DestroyImmediate(statusContainer.GetChild(statusContainer.childCount-1).gameObject);
        }

        for (int i = 0; i < effects.Count; i++)
        {
            Image icon=statusContainer.GetChild(i).GetComponent<Image>();
            if (icon != null)
            {
                if (effects[i].EffectIcon != null)
                {
                    icon.sprite=effects[i].EffectIcon;
                    icon.color=Color.white;
                }
                else
                {
                    icon.color=new Color(0,0,0,0);
                }
            }
        }
    }
}