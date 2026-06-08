using UnityEngine;

public class TutorialPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private bool showOnStart;

    private void Awake()
    {
        SetTutorialVisible(showOnStart);
    }

    public void ShowTutorial()
    {
        SetTutorialVisible(true);
    }

    public void HideTutorial()
    {
        SetTutorialVisible(false);
    }

    private void SetTutorialVisible(bool visible)
    {
        if (root == null)
        {
            return;
        }

        root.SetActive(visible);
    }
}
