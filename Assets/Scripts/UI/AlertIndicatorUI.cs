using UnityEngine;
using UnityEngine.UI;
using static EnemySystem.AICore;

[RequireComponent(typeof(RectTransform))]
public class AlertIndicatorUI : MonoBehaviour
{
    public RectTransform rectTransform;
    CanvasGroup _canvasGroup;
    
    [SerializeField] float maxFillAmount = .2f;
    [SerializeField] Image image1;
    [SerializeField] Image image2;

    [SerializeField] [Range(0,1)] float baseIndicatorOpacity = 0.3f;

    void Awake()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void SetAlertProgress(float value, AlertLevel alertLevel)
    {
        value = Mathf.Clamp(value, 0, 1);
        image1.fillAmount = value * maxFillAmount;
        image2.fillAmount = value * maxFillAmount;
        Color newColor = Color.clear;

        float opacity = baseIndicatorOpacity;

        switch (alertLevel)
        {
            case AlertLevel.None:
                newColor = Color.clear;
                break;
            case AlertLevel.Low:
                opacity /= 3;
                newColor = Color.white;
                break;
            case AlertLevel.Medium:
                newColor = Color.royalBlue;
                break;
            case AlertLevel.High:
                newColor = Color.yellow;
                break;
            case AlertLevel.Extreme:
                newColor = Color.red;
                break;
        }

        image1.color = newColor;
        image2.color = newColor;

        _canvasGroup.alpha = opacity;
    }
}