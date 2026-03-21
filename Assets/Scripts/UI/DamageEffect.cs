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
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public void OnPlayerHit(float damage)
    {
        if (damage <= 0) return;
        StopAllCoroutines(); 
        StartCoroutine(FadeOutVignette());
    }

    private IEnumerator FadeOutVignette()
    {
        float elapsedTime = 0f;
        
        damageVolume.weight = 1f;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            damageVolume.weight = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            yield return null;
        }
        
        damageVolume.weight = 0f;
    }
}