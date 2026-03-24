using System;
using System.Collections;
using PlayerShootingSystem;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class UICrosshair : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform crosshairRect;

    [SerializeField] CanvasGroup crosshairHitIndicator;
    [SerializeField] float hitFadeDuration = 0.5f;

    private Vector2 currentPos;
    private Vector2 targetPos;

    [Header("Crosshair leaves")]
    [SerializeField] RectTransform leafTR;
    [SerializeField] RectTransform leafTL;
    [SerializeField] RectTransform leafBR;
    [SerializeField] RectTransform leafBL;
    [SerializeField] private float baseLeafDistance = 10;
    [SerializeField] private float maxLeafDistance = 20;
    private float leafTargetPos;
    private float leafCurrentPos;

    [Header("Smoothing")]
    public float snappiness = 15f;
    public float returnSpeed = 5f;

    private void Awake()
    {
        leafCurrentPos = baseLeafDistance;
        leafTargetPos = baseLeafDistance;
    }

    void Update()
    {
        targetPos = Vector2.Lerp(targetPos, Vector2.zero, returnSpeed * Time.deltaTime);
        currentPos = Vector2.Lerp(currentPos, targetPos, snappiness * Time.deltaTime);
        
        // anchoredPosition assumes crosshair is anchored to the center of the screen
        crosshairRect.anchoredPosition = currentPos;
        
        leafTargetPos = Mathf.Lerp(leafTargetPos, baseLeafDistance, returnSpeed * Time.deltaTime);
        leafCurrentPos = Mathf.Lerp(leafCurrentPos, leafTargetPos, snappiness * Time.deltaTime);
            
        leafTR.anchoredPosition = new Vector2(leafCurrentPos, leafCurrentPos);
        leafTL.anchoredPosition = new Vector2(-leafCurrentPos, leafCurrentPos);
        leafBR.anchoredPosition = new Vector2(leafCurrentPos, -leafCurrentPos);
        leafBL.anchoredPosition = new Vector2(-leafCurrentPos, -leafCurrentPos);
    }

    public void ApplyRecoil(GunInfo gunInfo)
    {
        targetPos += new Vector2(
            Random.Range(-gunInfo.recoilHorizontal, gunInfo.recoilHorizontal), 
            gunInfo.recoilUpward
        );
    }

    public void SetSpread(float value)
    {
        value = Mathf.Clamp01(value*10f);
        leafTargetPos = baseLeafDistance + value * (maxLeafDistance - baseLeafDistance);
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

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }
}