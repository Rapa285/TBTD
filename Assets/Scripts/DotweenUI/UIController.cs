using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;


public class UIController : MonoBehaviour
{
    public RectTransform[] buttons;
    public float slideDuration = 0.6f;
    public float delayBetweenButtons = 0.2f;

    private void Start()
    {
        SlideInButtons();
    }

    private void SlideInButtons()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            RectTransform button = buttons[i];
            Vector2 originalPosition = button.anchoredPosition;
            button.anchoredPosition = new Vector2(-500f, originalPosition.y);
            button.DOAnchorPos(originalPosition, slideDuration)
                .SetDelay(i * delayBetweenButtons)
                .SetEase(Ease.OutBack);
        }
    }
}