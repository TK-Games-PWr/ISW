using PlayerShootingSystem;
using UnityEngine;

public class UICrosshairRecoil : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform crosshairRect;

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
}