using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoader : MonoBehaviour
{
    // public static SceneLoader Instance { get; private set; }

    public event Action<string> SceneLoaded;

    [SerializeField] Animator anim;
    [SerializeField] string transitionStartAnimTrigger = "LoadStart";
    [SerializeField] string transitionEndAnimTrigger = "LoadComplete";

    [SerializeField, Tooltip("Keep this loader alive across scene changes.")]
    bool keepAlive = true;

    bool ignoredFirstAnimationTrigger = false;

    bool hasPendingLoad;
    string pendingSceneName;
    int pendingBuildIndex = -1;

    void Awake()
    {
        // if (Instance != null && Instance != this)
        // {
        //     Destroy(gameObject);
        //     return;
        // }

        // Instance = this;
        RegisterWithServiceLocator();

        if (keepAlive)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void OnDestroy()
    {
        // if (Instance == this)
        // {
        //     Instance = null;
        //     ServiceLocator.Unregister<SceneLoader>(this);
        // }

        ServiceLocator.Unregister<SceneLoader>(this);

    }

    private void RegisterWithServiceLocator()
    {
        if (ServiceLocator.TryResolve<SceneLoader>(out SceneLoader existingSceneLoader)
            && existingSceneLoader != null
            && existingSceneLoader != this)
        {
            Debug.LogWarning(
                $"{nameof(SceneLoader)} on '{name}' replaced the previously registered {nameof(SceneLoader)} on '{existingSceneLoader.name}'.",
                this);
        }

        ServiceLocator.Register<SceneLoader>(this);
    }

    public void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneLoad.LoadScene called with an empty scene name.", this);
            return;
        }

        StartTransitionAnim();
        hasPendingLoad = true;
        pendingSceneName = sceneName;
        pendingBuildIndex = -1;
        SceneManager.LoadSceneAsync(sceneName, mode);
    }

    public void LoadScene(int buildIndex, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (buildIndex < 0)
        {
            Debug.LogWarning("SceneLoad.LoadScene called with an invalid build index.", this);
            return;
        }

        StartTransitionAnim();
        hasPendingLoad = true;
        pendingBuildIndex = buildIndex;
        pendingSceneName = null;
        SceneManager.LoadSceneAsync(buildIndex, mode);
    }

    public void LoadScene(SceneReferenceSO sceneReference, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (sceneReference == null || !sceneReference.IsValid)
        {
            Debug.LogWarning("SceneLoad.LoadScene called with an invalid SceneReferenceSO.", this);
            return;
        }

        if (sceneReference.BuildIndex >= 0)
        {
            LoadScene(sceneReference.BuildIndex, mode);
            return;
        }

        LoadScene(sceneReference.SceneName, mode);
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneLoaded?.Invoke(scene.name);

        if (!hasPendingLoad)
        {
            return;
        }

        bool matches = (pendingBuildIndex >= 0 && scene.buildIndex == pendingBuildIndex)
            || (!string.IsNullOrWhiteSpace(pendingSceneName) && scene.name == pendingSceneName);
        if (!matches)
        {
            return;
        }

        hasPendingLoad = false;
        pendingBuildIndex = -1;
        pendingSceneName = null;
        EndTransitionAnim();
    }

    public void StartTransitionAnim()
    {
        if (!ignoredFirstAnimationTrigger)
        {
            ignoredFirstAnimationTrigger = true;
            return;
        }
        if (anim)
        {
            anim.SetTrigger(transitionStartAnimTrigger);
        }
    }

    public void EndTransitionAnim()
    {
        if (anim)
        {
            anim.SetTrigger(transitionEndAnimTrigger);
        }
    }
}
