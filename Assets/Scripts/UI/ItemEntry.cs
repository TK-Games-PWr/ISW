using PlayerShootingSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemEntry : MonoBehaviour
{
    [SerializeField] Sprite defaultIcon;
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI indexText;
    [SerializeField] Image background;

    public void SetItem(GunInfo gun)
    {
        if (gun != null && gun.icon != null)
        {
            icon.sprite = gun.icon;
        }
        else
        {
            icon.sprite = defaultIcon;
        }
    }

    public void SetIndex(int index)
    {
        indexText.text = index.ToString();
    }

    public void SetBackground(Color color)
    {
        background.color = color;
    }
}