using UnityEngine;
using UnityEngine.UI;

public class ButtonSpriteToggle : MonoBehaviour
{
    public Image buttonImage;
    public Sprite playSprite;
    public Sprite pauseSprite;

    private bool isPaused = false;

    public void ToggleButton()
    {
        isPaused = !isPaused; 

        if (isPaused)
        {
            buttonImage.sprite = pauseSprite;
        }
        else
        {
            buttonImage.sprite = playSprite;
        }
    }
}
