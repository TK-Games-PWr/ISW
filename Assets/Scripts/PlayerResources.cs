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
    
    [SerializeField] GameObject DeathScreen;
    [SerializeField] TextMeshProUGUI HealthLabel;
    public List<AmmoEntry> playerAmmo;
    public float CurrentHealth { get; private set; }
    [SerializeField] float maxHealth = 100f;

    private bool isDead = false;

    void Awake()
    {
        CurrentHealth = maxHealth;
        OnHealthChanged += ShowHealth;
        ShowHealth(CurrentHealth);
        DeathScreen.SetActive(false);
        Time.timeScale = 1;
    }

    void OnDestroy()
    {
        OnHealthChanged -= ShowHealth;
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

    private void Die()
    {
        isDead = true;
        
        // Notify any listeners (like Game Manager or Animator) that the player died
        OnDeath?.Invoke(); 
        
        DeathScreen.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        GetComponent<PlayerActionsController>().enabled = false;
        Debug.Log("time stop");
        Time.timeScale = 0; // TODO replace with proper death handling
    }

    private void ShowHealth(float amount)
    {
        HealthLabel.text = (int)CurrentHealth + "hp";
    }
}
