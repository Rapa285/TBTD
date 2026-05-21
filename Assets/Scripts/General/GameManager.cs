using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private float baseHealth = 100f;

    [SerializeField]
    private SceneLoader sceneLoader;

    private GameObject SelectedTower;
    private TimeService timeService;

    private void Awake()
    {
        ResolveSceneLoader();
        ResolveTimeService();
    }

    private void OnEnable()
    {
        GeneralEventBus<BaseDamagedEvent>.Subscribe(DamageBase);
        GeneralEventBus<GamePausedEvent>.Subscribe(PauseGame);
        GeneralEventBus<GameUnPausedEvent>.Subscribe(UnPauseGame);
        GeneralEventBus<RetryGameEvent>.Subscribe(RetryGame);
        GeneralEventBus<ExitToMainMenuEvent>.Subscribe(ExitToMainMenu);
    }

    private void OnDisable()
    {
        GeneralEventBus<BaseDamagedEvent>.Unsubscribe(DamageBase);
        GeneralEventBus<GamePausedEvent>.Unsubscribe(PauseGame);
        GeneralEventBus<GameUnPausedEvent>.Unsubscribe(UnPauseGame);
        GeneralEventBus<RetryGameEvent>.Unsubscribe(RetryGame);
        GeneralEventBus<ExitToMainMenuEvent>.Unsubscribe(ExitToMainMenu);
    }

    private void DamageBase(BaseDamagedEvent eventData)
    {
        baseHealth -= eventData.DamageAmount;
        if (baseHealth <= 0)
        {
            GameOver();
        }
    }

    private void GameOver()
    {
        GeneralEventBus<GameOverEvent>.Publish(new GameOverEvent { });
        ResolveTimeService();
        if (timeService != null)
        {
            timeService.RequestPause(this, true);
        }
    }

    private void PauseGame(GamePausedEvent eventData)
    {
        ResolveTimeService();
        if (timeService != null)
        {
            timeService.RequestPause(this, true);
        }
    }

    private void UnPauseGame(GameUnPausedEvent eventData)
    {
        ResolveTimeService();
        if (timeService != null)
        {
            timeService.ReleasePause(this);
        }
    }

    private void RetryGame(RetryGameEvent eventData)
    {
        ResolveTimeService();
        if (timeService != null)
        {
            timeService.ReleasePause(this);
        }
        LoadScene("InGame");
    }

    private void ExitToMainMenu(ExitToMainMenuEvent eventData)
    {
        ResolveTimeService();
        if (timeService != null)
        {
            timeService.ReleasePause(this);
        }
        LoadScene("Title Screen");
    }

    private void ResolveSceneLoader()
    {
        if (sceneLoader == null)
        {
            ServiceLocator.TryResolve(out sceneLoader);
        }
    }

    private void ResolveTimeService()
    {
        if (timeService == null)
        {
            ServiceLocator.TryResolve(out timeService);
        }
    }

    async private void LoadScene(string sceneName)
    {
        ResolveSceneLoader();
        if (sceneLoader == null)
        {
            Debug.LogWarning($"{nameof(GameManager)} cannot load scene '{sceneName}' because no {nameof(SceneLoader)} was found.", this);
            return;
        }

        sceneLoader.LoadScene(sceneName);
    }
}
