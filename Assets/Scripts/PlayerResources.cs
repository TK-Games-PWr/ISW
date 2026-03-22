using System;
using System.Collections.Generic;
using PlayerShootingSystem;
using TK_Shared._3DPlayerMovement;
using TMPro;
using UnityEngine;
[System.Serializable]
public class AmmoEntry
{
    public AmmoType ammoType;
    public int amount;
}

public class PlayerResources : MonoBehaviour, ICharacter
{
    public event Action<float> OnHealthChanged;
    public event Action OnDeath;
    
    [SerializeField] TextMeshProUGUI HealthLabel;
    [SerializeField] GameObject WeaponHolder;
    public List<Gun> weapons;
    public List<AmmoEntry> playerAmmo;
    public float CurrentHealth { get; private set; }
    [SerializeField] float maxHealth = 100f;

    private bool isDead = false;
    public void PutWeaponInInventoryObject(GameObject weapon)
    {
        weapon.transform.parent = WeaponHolder.transform;
        weapon.transform.localPosition = Vector3.zero;
        
    }
    void Awake()
    {
        CurrentHealth = maxHealth;
        OnHealthChanged += ShowHealth;
        ShowHealth(CurrentHealth);
        Time.timeScale = 1;
    }

    private void Start()
    {
        OnHealthChanged += DamageEffect.Instance.OnPlayerHit;
        OnDeath += LevelManager.Instance.OnPlayerDeath;
        
        LevelManager.Instance.player = gameObject;
    }

    void OnDestroy()
    {
        OnHealthChanged -= ShowHealth;
        OnHealthChanged -= DamageEffect.Instance.OnPlayerHit;
    }

    /// <summary>
    /// Does damage to a player, heals if amount is negative
    /// </summary>
    public void Damage(float amount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0, maxHealth);
        
        OnHealthChanged?.Invoke(CurrentHealth);
        
        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        
        // Notify any listeners (like Game Manager or Animator) that the player died
        OnDeath?.Invoke(); 
    }

    void ShowHealth(float amount)
    {
        HealthLabel.text = (int)CurrentHealth + "hp";
    }


}
