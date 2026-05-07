using UnityEngine;
using UnityEngine.UI;

public class TitleScreen : MonoBehaviour
{
    public Button startButton;
    public SceneReferenceSO ingameScene;
    public SceneLoader sceneLoader;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        startButton.onClick.AddListener(OnStartButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        if (sceneLoader == null)
        {
            sceneLoader = ServiceLocator.TryResolve(out SceneLoader resolvedSceneLoader) ? resolvedSceneLoader : null;
        }
        sceneLoader.LoadScene(ingameScene.SceneName);
    }
}
