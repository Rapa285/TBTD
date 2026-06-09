using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;


public class MainmenuLoadIntro : MonoBehaviour
{
    private SceneLoader sceneLoader;
    
    [SerializeField]
    private SceneReferenceSO introScene;


    [SerializeField] private Image targetImage;
    [SerializeField] private float duration = 1f;

    private bool subscribed = false;

    public void FadeOut(string _txt)
    {
        // Starts at fully visible
        Color color = targetImage.color;
        color.a = 1f;
        targetImage.color = color;

        // Fades alpha from 1 to 0
        targetImage.DOFade(0f, duration);
    }

    void OnEnable()
    {
        sceneLoader = ServiceLocator.TryResolve(out SceneLoader resolvedSceneLoader) ? resolvedSceneLoader : null;
        if (sceneLoader == null)
        {
            return;    
        }

        // Try load the Intro scene additively
        if (introScene != null)
        {
            sceneLoader.SceneLoaded += FadeOut;
            subscribed = true;
            sceneLoader.LoadScene(introScene, UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }
        
    }

    void OnDestroy()
    {
        if (subscribed)
        {
            sceneLoader.SceneLoaded -= FadeOut;
        }
    }
}