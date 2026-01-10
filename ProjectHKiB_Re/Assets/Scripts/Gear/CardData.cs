using System;
using UnityEngine;

[Serializable]
public class CardData
{
    public string cardName;
    public GearDataSO[] mergedGearList;
    [field: SerializeField] public Gear[] GearList {get; private set;}
    public void SetGear(int cardIndex, int slotIndex, Gear gear)
    {
        if (slotIndex >= 0 && slotIndex < GameManager.instance.gearManager.MaxGearSlotCount)
        {
            if (GearList[slotIndex].data != null) GearList[slotIndex].UnequipTo(cardIndex);
            GearList[slotIndex] = gear;
            MergeGear();
        }
    }
    public void ResetGear(int cardIndex, int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < GameManager.instance.gearManager.MaxGearSlotCount)
        {
            GearList[slotIndex].UnequipTo(cardIndex);
            GearList[slotIndex] = new(null);
            MergeGear();
        }
    }
    public void Initialize()
    {
        int max = GameManager.instance.gearManager.PhysicalMaxGearSlotCount;
        GearList = new Gear[max];
        mergedGearList = new GearDataSO[max];
        for (int i = 0; i < max; i++) GearList[i] = new(null);
        MergeGear();
    }

    public void MergeGear() => GameManager.instance.gearManager.MergeGear(this);

    public Sprite GetMergedIcon(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < mergedGearList.Length)
        {
            return mergedGearList[slotIndex].itemIcon;
        }
        else return GameManager.instance.gearManager.DefaultGearData.itemIcon;
    }

    public Sprite GetIcon(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < mergedGearList.Length)
        {
            return GearList[slotIndex].data.itemIcon;
        }
        else return GameManager.instance.gearManager.DefaultGearData.itemIcon;
    }
}