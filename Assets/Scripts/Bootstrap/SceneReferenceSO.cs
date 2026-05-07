using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "ASCENTA/Scene Reference", fileName = "SceneReference")]
public sealed class SceneReferenceSO : ScriptableObject
{
    [SerializeField] string sceneName;
    [SerializeField] int buildIndex = -1;
#if UNITY_EDITOR
    [SerializeField] SceneAsset sceneAsset;
#endif

    public string SceneName => sceneName;
    public int BuildIndex => buildIndex;
    public bool IsValid => buildIndex >= 0 || !string.IsNullOrWhiteSpace(sceneName);

#if UNITY_EDITOR
    void OnValidate()
    {
        if (sceneAsset == null)
        {
            sceneName = string.Empty;
            buildIndex = -1;
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(sceneAsset);
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            sceneName = string.Empty;
            buildIndex = -1;
            return;
        }

        sceneName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        buildIndex = GetBuildIndexForScene(assetPath);
    }

    static int GetBuildIndexForScene(string scenePath)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path == scenePath)
            {
                return i;
            }
        }

        return -1;
    }
#endif
}
