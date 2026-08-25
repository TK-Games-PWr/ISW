using UnityEngine;
using UnityEngine.UI;
using static EnemySystem.EnemyBrain;

[RequireComponent(typeof(RectTransform))]
public class AlertIndicatorUI : MonoBehaviour
{
    public RectTransform rectTransform;
    CanvasGroup _canvasGroup;
    
    [SerializeField] float maxFillAmount = .2f;
    [SerializeField] Image image1;
    [SerializeField] Image image2;

    [SerializeField] [Range(0,1)] float baseIndicatorOpacity = 0.3f;

    float _targetValue;

    void Awake()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        image1.fillAmount = 0f;
        image2.fillAmount = 0f;
    }

    void Update()
    {
        float fillAmount = Mathf.Lerp(image1.fillAmount, _targetValue, 
            Time.deltaTime / Mathf.Clamp(Mathf.Abs(_targetValue - image1.fillAmount)*10f, 0.01f, 1f));
        image1.fillAmount = fillAmount;
        image2.fillAmount = fillAmount;
    }

    public void SetAlertProgress(float value, AlertLevel alertLevel)
    {
        value = Mathf.Clamp(value, 0, 1);
        _targetValue = value * maxFillAmount;
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