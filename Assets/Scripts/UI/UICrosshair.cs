using System.Collections;
using PlayerShootingSystem;
using UnityEngine;

public class UICrosshair : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform crosshairRect;

    [SerializeField] CanvasGroup crosshairHitIndicator;
    [SerializeField] float hitFadeDuration = 0.5f;

    private Vector2 currentPos;
    private Vector2 targetPos;

    [Header("Smoothing")]
    public float snappiness = 15f;
    public float returnSpeed = 5f;

    void Update()
    {
        targetPos = Vector2.Lerp(targetPos, Vector2.zero, returnSpeed * Time.deltaTime);
        currentPos = Vector2.Lerp(currentPos, targetPos, snappiness * Time.deltaTime);
        
        // anchoredPosition assumes crosshair is anchored to the center of the screen
        crosshairRect.anchoredPosition = currentPos;
    }

    public void ApplyRecoil(GunInfo gunInfo)
    {
        targetPos += new Vector2(
            Random.Range(-gunInfo.recoilHorizontal, gunInfo.recoilHorizontal), 
            gunInfo.recoilUpward
        );
    }

    public void ShowHit()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateHit());
    }

    private IEnumerator AnimateHit()
    {
        float elapsedTime = 0f;
        crosshairHitIndicator.alpha = 1f;
        
        while (elapsedTime < hitFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            crosshairHitIndicator.alpha = Mathf.Lerp(1f, 0f, elapsedTime / hitFadeDuration);
            yield return null;
        }
        
        crosshairHitIndicator.alpha = 0f;
    }
}