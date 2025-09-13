using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GearPanel : MonoBehaviour
{
    public Image icon9x9;
    public Image icon5x5;
    public TextMeshProUGUI itemName;
    public Image itemColor;
    public ItemTooltipContent itemTooltip;
    public Gear gear;

    public void SetData(Gear gear)
    {
        this.gear = gear;
        if (icon9x9) icon9x9.sprite = gear.data.itemIcon9x9;
        if (icon5x5 && gear.data.parentProperties != null && gear.data.parentProperties.Length > 0)
            icon5x5.sprite = gear.data.parentProperties[0].icon5x5;
        if (itemName) itemName.text = gear.data.name;
        if (itemColor) itemColor.color = gear.data.color;
        if (itemTooltip) itemTooltip.SetData(gear.data);
    }
}