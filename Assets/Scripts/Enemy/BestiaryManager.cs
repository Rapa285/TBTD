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
    [SerializeField] 
    private ModelViewer modelViewer; 



    [Header("Left Panel")]
    [SerializeField] private TextMeshProUGUI revealedEnemyCountText;

    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform buttonContainer;

    [SerializeField] private bool resetBestiaryOnStart = false;


    private HashSet<EnemyType> revealedEnemies = new HashSet<EnemyType>();
    private Dictionary<EnemyType, GameObject> spawnedButtons = new Dictionary<EnemyType, GameObject>();
    private int revealedEnemyCount = 0;

    private bool openedFromSettings = false;

    private TimeService timeService;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void ResolveTimeService()
    {
        if (timeService == null)
        {
            ServiceLocator.TryResolve(out timeService);
        }
    }

    private void Start()
    {
        if (resetBestiaryOnStart) ClearRevealedEnemies();
        LoadRevealedEnemies();
        GenerateEnemyList();
        
        if (bestiaryPanel != null) bestiaryPanel.SetActive(false);
    }

    public void OpenBestiaryForEnemy(EnemyType type)
    {
        if (bestiaryPanel != null) bestiaryPanel.SetActive(true);
        ResolveTimeService();
        if (timeService != null)
        {
            timeService.RequestPause(this, true);
        }
        BestiaryOpenEntry(type);
    }

    public void OpenBestiary()
    {
        openedFromSettings = true;
        if (bestiaryPanel != null) bestiaryPanel.SetActive(true);
        BestiaryOpenEntry(EnemyType.Normal);
    }

    public void CloseBestiary()
    {
        if (bestiaryPanel != null) bestiaryPanel.SetActive(false);
        
        if (modelViewer != null) modelViewer.ClearModel();

        if (openedFromSettings)
        {
            openedFromSettings = false;
        }
        else
        {
            ResolveTimeService();
            if (timeService != null)
            {
                timeService.ReleasePause(this);
            }
            
        }
    }

    public void BestiaryRevealEnemy(EnemyType type)
    {
        PlayerPrefs.SetInt($"Bestiary_Unlocked_{type}", 1);
        PlayerPrefs.Save();

        if (!revealedEnemies.Contains(type))
        {
            revealedEnemies.Add(type);
            revealedEnemyCount++;
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

                if (modelViewer != null) modelViewer.DisplayModel(data);
            }
            else
            {
                detailNameText.text = "???";
                detailDescriptionText.text = "Enemy has not been encountered yet.";
                
                if (modelViewer != null) modelViewer.ClearModel();
            }
        }
    }

    private void LoadRevealedEnemies()
    {
        revealedEnemies.Clear();
        revealedEnemyCount = 0;
        foreach (EnemyDataSO data in enemyDatabase)
        {
            if (PlayerPrefs.GetInt($"Bestiary_Unlocked_{data.enemyType}", 0) == 1)
            {
                revealedEnemies.Add(data.enemyType);
                revealedEnemyCount++;
            }
        }
    }

    private void ClearRevealedEnemies()
    {
        revealedEnemies.Clear();
        foreach (EnemyDataSO data in enemyDatabase)
        {
            PlayerPrefs.SetInt($"Bestiary_Unlocked_{data.enemyType}", 0);
        }
        PlayerPrefs.Save();
        revealedEnemyCount = 0;
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

            string displayName = revealedEnemies.Contains(enemyData.enemyType) ? enemyData.enemyName : "???";

            if (tmpText != null) tmpText.text = displayName;
            else if (legacyText != null) legacyText.text = displayName;
        }

        RefreshRevealedEnemyCountUI();
    }

    private void RefreshRevealedEnemyCountUI()
    {
        if (revealedEnemyCountText != null)
        {
            revealedEnemyCountText.text = $"{revealedEnemyCount} / {enemyDatabase.Count}";
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