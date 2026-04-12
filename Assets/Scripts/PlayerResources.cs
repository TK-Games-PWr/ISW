using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

[Serializable]
public class WeaponInventory : IEnumerable<Gun>
{
    public List<Gun> items;

    public Action<int, Gun> OnSlotChanged;

    public WeaponInventory(int size)
    {
        items = new List<Gun>(new Gun[size]);
    }

    public Gun this[int i]
    {
        get => items[i];
        set
        {
            if (items[i] == value) return;
            items[i] = value;
            OnSlotChanged?.Invoke(i, value);
        }
    }

    public int Count => items.Count;

    // To allow IEnumerable operations and methods
    public IEnumerator<Gun> GetEnumerator() => items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class PlayerResources : MonoBehaviour, ICharacter
{
    public event Action<float> OnHealthChanged;
    public event Action OnDeath;

    [SerializeField] private TextMeshProUGUI HealthLabel;
    [SerializeField] private GameObject WeaponHolder;
    public WeaponHotbar hotbar;
    List<Gun> _weapons;

    public WeaponInventory weapons = new (4);
    public List<AmmoEntry> playerAmmo;
    public GameObject knifeObj;

    public float CurrentHealth { get; private set; }
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool godMode;

    [Header("Passive Health Regeneration")] [SerializeField]
    private float regenRate = 10f;

    [SerializeField] private float regenDelay = 5f;
    [SerializeField] private bool canRegenerate = true;

    private float lastDamageTime;
    private bool isRegenerating;

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

    void Start()
    {
        OnHealthChanged += DamageEffect.Instance.OnPlayerHit;
        OnDeath += LevelManager.Instance.OnPlayerDeath;
        weapons.OnSlotChanged += HandleSlotChanged;

        hotbar.UpdateItems(weapons.items);

        LevelManager.Instance.player = gameObject;
    }

    void OnDestroy()
    {
        OnHealthChanged -= ShowHealth;
        OnHealthChanged -= DamageEffect.Instance.OnPlayerHit;
        weapons.OnSlotChanged -= HandleSlotChanged;
    }
    
    private void HandleSlotChanged(int index, Gun gun)
    {
        if (hotbar != null)
        {
            hotbar.UpdateItem(gun, index);
        }
    }

    private void Update()
    {
        if (!canRegenerate || CurrentHealth >= maxHealth)
            return;

        if (Time.time - lastDamageTime >= regenDelay)
        {
            RegenerateHealth();
        }
    }

    public void Damage(float amount)
    {
#if UNITY_EDITOR
        if (godMode) return;
#endif
        CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0, maxHealth);

        if (amount > 0)
        {
            lastDamageTime = Time.time;
        }

        OnHealthChanged?.Invoke(amount);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void RegenerateHealth()
    {
        float healAmount = regenRate * Time.deltaTime;
        CurrentHealth = Mathf.Min(CurrentHealth + healAmount, maxHealth);

        OnHealthChanged?.Invoke(-healAmount);
    }

    void Die()
    {
        OnDeath?.Invoke();
    }

    void ShowHealth(float damageAmount)
    {
        HealthLabel.text = Mathf.CeilToInt(CurrentHealth) + "hp";
    }
}