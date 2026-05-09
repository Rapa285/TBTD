using UnityEditor;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private float baseHealth = 100f;
    private GameObject SelectedTower;
    [SerializeField] SceneLoader sceneLoader;

    private void Awake()
    {
        if (sceneLoader == null)
        {
            // sceneLoader = FindFirstObjectByType<SceneLoader>(FindObjectsInactive.Include);
            sceneLoader = ServiceLocator.TryResolve(out SceneLoader resolvedSceneLoader) ? resolvedSceneLoader : sceneLoader;
        }
    }

    private void OnEnable()
    {
        // Mulai mendengarkan event BaseDamagedEvent
        GeneralEventBus<BaseDamagedEvent>.Subscribe(DamageBase);
        GeneralEventBus<GamePausedEvent>.Subscribe(PauseGame);
        GeneralEventBus<GameUnPausedEvent>.Subscribe(UnPauseGame);
        GeneralEventBus<RetryGameEvent>.Subscribe(RetryGame);
        GeneralEventBus<ExitToMainMenuEvent>.Subscribe(ExitToMainMenu);
    }

    private void OnDisable()
    {
        // Mulai mendengarkan event BaseDamagedEvent
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

    // Menyiarkan Event Game Over
    private void GameOver()
    {
        GeneralEventBus<GameOverEvent>.Publish(new GameOverEvent{});
        // GeneralEventBus<GamePausedEvent>.Publish(new GamePausedEvent{});
    }

    private void PauseGame(GamePausedEvent eventData)
    {
        Time.timeScale = 0f;
    }

    private void UnPauseGame(GameUnPausedEvent eventData)
    {
        Time.timeScale = 1f;
    }


    private void RetryGame(RetryGameEvent eventData)
    {
        sceneLoader.LoadScene("InGame");
        // UnPauseGame(new GameUnPausedEvent{});
    }

    private void ExitToMainMenu(ExitToMainMenuEvent eventData)
    {
        sceneLoader.LoadScene("Title Screen");
    }
}
