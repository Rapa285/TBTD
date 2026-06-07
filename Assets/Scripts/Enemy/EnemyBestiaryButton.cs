using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class EnemyBestiaryButton : MonoBehaviour
{
    private Button infoButton;
    private EnemyEntity enemyEntity;

    private void Awake()
    {
        infoButton = GetComponent<Button>();
        enemyEntity = GetComponentInParent<EnemyEntity>();
        infoButton.onClick.AddListener(OnInfoButtonClicked);
    }

    private void OnInfoButtonClicked()
    {
        if (enemyEntity != null && enemyEntity.enemyData != null)
        {
            BestiaryManager.Instance.BestiaryRevealEnemy(enemyEntity.enemyData.enemyType);
            BestiaryManager.Instance.OpenBestiaryForEnemy(enemyEntity.enemyData.enemyType);
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("There's no enemy data");
        }
    }

    private void OnDestroy()
    {
        if (infoButton != null)
        {
            infoButton.onClick.RemoveListener(OnInfoButtonClicked);
        }
    }

    private void OnEnable()
    {
        if (enemyEntity != null && enemyEntity.enemyData != null)
        {
            int isUnlocked = PlayerPrefs.GetInt($"Bestiary_Unlocked_{enemyEntity.enemyData.enemyType}", 0);
            
            if (isUnlocked == 1)
            {
                gameObject.SetActive(false);
            }
        }
    }
}