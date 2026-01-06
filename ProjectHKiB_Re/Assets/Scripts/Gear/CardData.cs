using System;
using UnityEngine;

[Serializable]
public class CardData
{
    public string cardName;
    public GearDataSO[] mergedGearList;
    public Gear[] gearList;
    public void SetGear(int cardIndex, int slotIndex, Gear gear)
    {
        if (slotIndex >= 0 && slotIndex < GameManager.instance.gearManager.MaxGearSlotCount)
        {
            if (gearList[slotIndex].data != null) gearList[slotIndex].UnequipTo(cardIndex);
            gearList[slotIndex] = gear;
            MergeGear();
        }
    }
    public void ResetGear(int cardIndex, int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < GameManager.instance.gearManager.MaxGearSlotCount)
        {
            gearList[slotIndex].UnequipTo(cardIndex);
            gearList[slotIndex] = new(null);
            MergeGear();
        }
    }
    public void Initialize()
    {
        int max = GameManager.instance.gearManager.PhysicalMaxGearSlotCount;
        gearList = new Gear[max];
        mergedGearList = new GearDataSO[max];
        for (int i = 0; i < max; i++) gearList[i] = new(null);
        MergeGear();
    }

    public void MergeGear() => GameManager.instance.gearManager.MergeGear(this);

    public Sprite GetMergedIcon(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < mergedGearList.Length)
        {
            return mergedGearList[slotIndex].itemIcon9x9;
        }
        else return GameManager.instance.gearManager.DefaultGearData.itemIcon9x9;
    }

    public Sprite GetIcon(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < mergedGearList.Length)
        {
            return gearList[slotIndex].data.itemIcon9x9;
        }
        else return GameManager.instance.gearManager.DefaultGearData.itemIcon9x9;
    }
}