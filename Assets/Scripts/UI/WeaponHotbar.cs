using System.Collections.Generic;
using PlayerShootingSystem;
using UnityEngine;

public class WeaponHotbar : MonoBehaviour
{
    [SerializeField] GameObject itemEntryPrefab;
    public List<ItemEntry> items;
    [SerializeField] int totalSlots;

    [Tooltip("Base color for item slot")] [SerializeField]
    Color baseColor;

    [Tooltip("Color of the highlight for item slot")] [SerializeField]
    Color highlightColor;

    void Awake()
    {
        for (int i = 1; i <= totalSlots; i++)
        {
            GameObject item = Instantiate(itemEntryPrefab, transform);
            item.name = "ItemEntry_" + i;
            var itemEntry = item.GetComponent<ItemEntry>();
            itemEntry.SetIndex(i);
            itemEntry.SetBackground(baseColor);
            items.Add(itemEntry);
        }
    }

    public void HighlightSlot(int index)
    {
        for (int i = 0; i < totalSlots; i++)
        {
            if (i == index)
            {
                items[i].SetBackground(highlightColor);
            }
            else
            {
                items[i].SetBackground(baseColor);
            }
        }
    }

    public void UpdateItem(Gun gun, int index)
    {
        items[index].SetItem(gun?.gunInfo);
    }

    public void UpdateItems(List<Gun> weapons)
    {
        for (int i = 0; i < totalSlots; i++)
        {
            items[i].SetItem(weapons[i]?.gunInfo);
        }
    }
}