using UnityEngine;
using UnityEngine.Rendering; // Required to access the Volume component
using System.Collections;

public class DamageEffect : MonoBehaviour
{
    public static DamageEffect Instance { get; private set; }
    
    [Tooltip("Drag your Damage Volume here")]
    [SerializeField] Volume damageVolume;
    
    [Tooltip("How long the fade out takes in seconds")]
    [SerializeField] float fadeDuration = 0.5f;
    
    [Tooltip("What is considered lowest damage for vignette")]
    [SerializeField] float lowDmgAmount = 10f;
    [Tooltip("What is considered high damage for boldest vignette")]
    [SerializeField] float highDmgAmount = 60f;

    void Awake()
    {
        if(Instance && Instance != this) Destroy(gameObject);
        Instance = this;
    }
    
    public void OnPlayerHit(float damage)
    {
        if (damage < lowDmgAmount) return;
        StopAllCoroutines(); 
        StartCoroutine(FadeOutVignette(damage));
    }

    IEnumerator FadeOutVignette(float damage)
    {
        float elapsedTime = 0f;
        float baseMult = Mathf.Lerp(0.6f, 1f, Mathf.InverseLerp(lowDmgAmount, highDmgAmount, damage));
        damageVolume.weight = baseMult;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            damageVolume.weight = Mathf.Lerp(baseMult, 0f, elapsedTime / fadeDuration);
            yield return null;
        }
        
        damageVolume.weight = 0f;
    }
}