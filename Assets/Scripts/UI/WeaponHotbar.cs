using System.Collections.Generic;
using UnityEngine;

public class WeaponHotbar : MonoBehaviour
{
    [SerializeField] GameObject itemEntryPrefab;
    public List<ItemEntry> items;
    [SerializeField] int totalSlots;

    void Start()
    {
        for (int i = 1; i <= totalSlots; i++)
        {
            GameObject itemEntry = Instantiate(itemEntryPrefab, transform);
            itemEntry.name = "ItemEntry_" + i;
            itemEntry.GetComponent<ItemEntry>().SetIndex(i);
            items.Add(itemEntry.GetComponent<ItemEntry>());
        }
    }
}
