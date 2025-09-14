using Michsky.MUIP;
using UnityEngine;

public class ItemTooltipContent : TooltipContent
{
    public bool smallMode;
    public bool icon9x9Only;
    public void SetData(ItemDataSO data)
    {
        icon5x5s.Clear();
        if (icon9x9Only)
        {
            icon9x9 = data.itemIcon9x9;
            image36x36 = null;
            description = "";
            return;
        }
        if (smallMode)
        {
            icon9x9 = data.itemIcon9x9;
            image36x36 = null;
        }
        else
        {
            icon9x9 = null;
            image36x36 = data.itemImage36x36;
        }

        for (int i = 0; i < data.parentProperties.Length; i++)
        {
            icon5x5s.Add(data.parentProperties[i].icon5x5);
        }

        description = data.name + "\n" + data.description;
    }
}