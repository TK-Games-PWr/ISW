using UnityEngine;
using UnityEngine.UI;
using static EnemySystem.AICore;

[RequireComponent(typeof(RectTransform))]
public class AlertIndicatorUI : MonoBehaviour
{
    public RectTransform rectTransform;

    [SerializeField] Image image1;
    [SerializeField] Image image2;

    private void Awake()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
    }

    public void SetAlertProgress(float value, AlertLevel alertLevel)
    {
        value = Mathf.Clamp(value, 0, 1);
        image1.fillAmount = value / 5f;
        image2.fillAmount = value / 5f;
        Color newColor = Color.clear;

        switch (alertLevel)
        {
            case AlertLevel.None:
                newColor = Color.clear;
                break;
            case AlertLevel.Low:
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
    }
}