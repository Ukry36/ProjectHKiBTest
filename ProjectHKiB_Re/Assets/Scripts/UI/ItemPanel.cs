using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ItemPanel : MonoBehaviour
{
    public Image icon9x9;
    public Image icon5x5;
    public TextMeshProUGUI itemName;
    public Image itemColor;
    public ItemTooltipContent itemTooltip;
    public Item item;
    private int index;

    public void SetData(Item item)
    {
        this.item = item;
        if (icon9x9) icon9x9.sprite = item.data.itemIcon9x9;
        if (icon5x5 && item.data.parentProperties != null && item.data.parentProperties.Length > 0)
            icon5x5.sprite = item.data.parentProperties[0].icon5x5;
        if (itemName)
        {
            if (item.data.canStack) itemName.text = item.data.name + " x" + item.Count;
            else itemName.text = item.data.name;
        }
        if (itemColor) itemColor.color = item.data.color;
        if (itemTooltip) itemTooltip.SetData(item.data);
    }
}