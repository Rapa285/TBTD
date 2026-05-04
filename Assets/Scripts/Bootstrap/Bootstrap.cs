using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Should load first no matter what
[DefaultExecutionOrder(-1000)]
public class Bootstrap : MonoBehaviour
{
    [Header("Scene Load")]
    [SerializeField] SceneReferenceSO nextScene;
    [SerializeField] bool loadAdditive;
    [SerializeField] bool keepBootstrapSceneLoaded = true;

    [Header("References")]
    [SerializeField] ConfigWorker configWorker;
    [SerializeField] SceneLoader sceneLoad;
    // [SerializeField] ServiceRegistry serviceRegistry;

    static bool hasBootstrapped;

    void Awake()
    {
        if (hasBootstrapped)
        {
            Destroy(gameObject);
            return;
        }

        hasBootstrapped = true;

        // if (configWorker == null)
        // {
        //     configWorker = FindFirstObjectByType<ConfigWorker>(FindObjectsInactive.Include);
        // }

        if (sceneLoad == null)
        {
            // sceneLoad = FindFirstObjectByType<SceneLoad>(FindObjectsInactive.Include);
            sceneLoad = ServiceLocator.TryResolve(out SceneLoader resolvedSceneLoad) ? resolvedSceneLoad : sceneLoad;
        }

        // if (serviceRegistry == null)
        // {
        //     serviceRegistry = FindFirstObjectByType<ServiceRegistry>(FindObjectsInactive.Include);
        // }

        if (keepBootstrapSceneLoaded)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    IEnumerator Start()
    {
        if (configWorker != null)
        {
            configWorker.EnsureInitialized();
        }
        else
        {
            Debug.LogWarning("Bootstrap could not find a ConfigWorker in the bootstrap scene.", this);
        }

        yield return null;

        // if (serviceRegistry != null && !serviceRegistry.InitialServicesReady)
        // {
        //     yield return serviceRegistry.LoadInitialServices();
        // }

        LoadNextScene();
    }

    void LoadNextScene()
    {
        if (nextScene != null)
        {
            LoadSceneMode mode = loadAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            if (nextScene.BuildIndex >= 0)
            {
                if (sceneLoad == null)
                {
                    Debug.LogError("Bootstrap requires a SceneLoad reference to load scenes.", this);
                    return;
                }

                sceneLoad.LoadScene(nextScene.BuildIndex, mode);
                return;
            }

            if (!string.IsNullOrWhiteSpace(nextScene.SceneName))
            {
                if (sceneLoad == null)
                {
                    Debug.LogError("Bootstrap requires a SceneLoad reference to load scenes.", this);
                    return;
                }

                sceneLoad.LoadScene(nextScene.SceneName, mode);
                return;
            }
        }

        if (nextScene == null)
        {
            Debug.LogWarning("Bootstrap has no next scene configured. Assign a SceneReferenceSO.", this);
            return;
        }

        Debug.LogWarning("Bootstrap has an invalid SceneReferenceSO (not in build settings).", this);
    }
}
