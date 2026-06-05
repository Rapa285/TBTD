using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BestiaryManager : MonoBehaviour
{
    public static BestiaryManager Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private List<EnemyDataSO> enemyDatabase;

    [Header("UI Panels")]
    [SerializeField] private GameObject bestiaryPanel; 

    [Header("Right Panel")]
    [SerializeField] private TextMeshProUGUI detailNameText;
    [SerializeField] private TextMeshProUGUI detailDescriptionText;
    [SerializeField] private RawImage modelRenderDisplay; 

    [Header("Left Panel")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform buttonContainer;

    [Header("3D Studio References")]
    [SerializeField] private Transform modelSpawnPoint; 
    
    private GameObject currentDisplayModel; 
    private HashSet<EnemyType> revealedEnemies = new HashSet<EnemyType>();
    private Dictionary<EnemyType, GameObject> spawnedButtons = new Dictionary<EnemyType, GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        LoadRevealedEnemies();
        GenerateEnemyList();
        
        if (bestiaryPanel != null) bestiaryPanel.SetActive(false);
    }

    public void OpenBestiaryForEnemy(EnemyType type)
    {
        if (bestiaryPanel != null) bestiaryPanel.SetActive(true);
        
        Time.timeScale = 0f;

        BestiaryOpenEntry(type);
    }

    public void CloseBestiary()
    {
        if (bestiaryPanel != null) bestiaryPanel.SetActive(false);
        
        SpawnDisplayModel(null);

        Time.timeScale = 1f;
    }

    public void BestiaryRevealEnemy(EnemyType type)
    {
        PlayerPrefs.SetInt($"Bestiary_Unlocked_{type}", 1);
        PlayerPrefs.Save();

        if (!revealedEnemies.Contains(type))
        {
            revealedEnemies.Add(type);
            RefreshEnemyListUI(); 
        }
    }

    public void BestiaryOpenEntry(EnemyType type)
    {
        EnemyDataSO data = enemyDatabase.Find(e => e.enemyType == type);
        
        if (data != null)
        {
            if (revealedEnemies.Contains(type))
            {
                detailNameText.text = data.enemyName;
                detailDescriptionText.text = data.specialAbilityDescription;

                if (modelRenderDisplay != null) modelRenderDisplay.gameObject.SetActive(true);
                
                SpawnDisplayModel(data.displayModelPrefab);
            }
            else
            {
                detailNameText.text = "???";
                detailDescriptionText.text = "Enemy has not been encountered yet.";
                if (modelRenderDisplay != null) modelRenderDisplay.gameObject.SetActive(false);
                SpawnDisplayModel(null);
            }
        }
    }

    private void SpawnDisplayModel(GameObject prefabToSpawn)
    {
        if (currentDisplayModel != null) Destroy(currentDisplayModel);

        if (prefabToSpawn != null && modelSpawnPoint != null)
        {
            currentDisplayModel = Instantiate(prefabToSpawn, modelSpawnPoint.position, modelSpawnPoint.rotation);
            currentDisplayModel.transform.SetParent(modelSpawnPoint);
        }
    }

    private void LoadRevealedEnemies()
    {
        revealedEnemies.Clear();
        foreach (EnemyDataSO data in enemyDatabase)
        {
            if (PlayerPrefs.GetInt($"Bestiary_Unlocked_{data.enemyType}", 0) == 1)
            {
                revealedEnemies.Add(data.enemyType);
            }
        }
    }
    

    private void GenerateEnemyList()
    {
        foreach (Transform child in buttonContainer) Destroy(child.gameObject);
        spawnedButtons.Clear();

        foreach (EnemyDataSO enemyData in enemyDatabase)
        {
            if (enemyData == null) continue;

            GameObject newBtnObj = Instantiate(buttonPrefab, buttonContainer);
            spawnedButtons.Add(enemyData.enemyType, newBtnObj);
            
            Button btn = newBtnObj.GetComponent<Button>();
            EnemyType capturedType = enemyData.enemyType; 
            
            btn.onClick.AddListener(() => BestiaryOpenEntry(capturedType));
        }
        
        RefreshEnemyListUI();
    }

    private void RefreshEnemyListUI()
    {
        foreach (EnemyDataSO enemyData in enemyDatabase)
        {
            if (enemyData == null || !spawnedButtons.ContainsKey(enemyData.enemyType)) continue;

            GameObject btnObj = spawnedButtons[enemyData.enemyType];
            
            TextMeshProUGUI tmpText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            Text legacyText = btnObj.GetComponentInChildren<Text>();

            string displayName = enemyData.enemyName;

            if (tmpText != null)
            {
                tmpText.text = displayName;
            }
            else if (legacyText != null)
            {
                legacyText.text = displayName;
            }
            else
            {
                Debug.LogWarning($"There's no text component on the button prefab for {enemyData.enemyName}");
            }
        }
    }

    private void OnEnable()
    {
        GeneralEventBus<EnemySpawnedEvent>.Subscribe(HandleEnemySpawned);
    }

    private void OnDisable()
    {
        GeneralEventBus<EnemySpawnedEvent>.Unsubscribe(HandleEnemySpawned);
    }

    private void HandleEnemySpawned(EnemySpawnedEvent evt)
    {
        BestiaryRevealEnemy(evt.EnemyType);
    }
}