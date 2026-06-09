using UnityEngine;
using UnityEngine.UI;

public class TitleScreen : MonoBehaviour
{
    public Button startButton;
    public Button exitButton;
    public SceneReferenceSO ingameScene;
    public SceneLoader sceneLoader;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        startButton.interactable = true;
        exitButton.interactable = true;


        exitButton.onClick.RemoveAllListeners();
        startButton.onClick.RemoveAllListeners();

        startButton.onClick.AddListener(OnStartButtonClicked);
        exitButton.onClick.AddListener(ExitGame);

    }


    private void OnStartButtonClicked()
    {
        if (sceneLoader == null)
        {
            sceneLoader = ServiceLocator.TryResolve(out SceneLoader resolvedSceneLoader) ? resolvedSceneLoader : null;
        }
        startButton.interactable = false;
        sceneLoader.LoadScene(ingameScene.SceneName);
    }

    private void ExitGame()
    {
        exitButton.interactable = false;
        Application.Quit();
    }
}
