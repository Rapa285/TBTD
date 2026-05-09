using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small reusable icon presenter for upgrade-driven UI.
/// </summary>
public class GenericIconDisplay : MonoBehaviour
{
    [SerializeField, Tooltip("Optional root toggled when the display has an icon. Defaults to this object.")]
    private GameObject root;

    [SerializeField, Tooltip("Image used to display the bound sprite.")]
    private Image image;

    public Image Image => image;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void Bind(UpgradeSO upgrade)
    {
        Bind(upgrade != null ? upgrade.Icon : null);
    }

    public void Bind(Sprite sprite)
    {
        ResolveReferences();

        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.enabled = sprite != null;
        SetRootVisible(sprite != null);
    }

    public void Clear()
    {
        Bind((Sprite)null);
    }

    private void ResolveReferences()
    {
        if (root == null)
        {
            root = gameObject;
        }

        if (image == null)
        {
            image = GetComponent<Image>();
        }
    }

    private void SetRootVisible(bool isVisible)
    {
        GameObject target = root != null ? root : gameObject;
        if (target != null && target.activeSelf != isVisible)
        {
            target.SetActive(isVisible);
        }
    }
}
