using Michsky.MUIP;
using UnityEngine;

public class ItemTooltipContent : TooltipContent
{
    public bool smallMode;
    public void SetData(ItemDataSO data)
    {
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

        icon5x5s = new Sprite[data.parentProperties.Length];
        for (int i = 0; i < data.parentProperties.Length; i++)
        {
            icon5x5s[i] = data.parentProperties[i].icon5x5;
        }

        description = data.name + "\n" + data.description;
    }
}