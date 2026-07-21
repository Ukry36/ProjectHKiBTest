using System;
using UnityEngine;

[Serializable]
public class Card
{
    public string cardName;
    [field: SerializeField] public Gear[] GearList { get; private set; }
    public void SetGear(int cardIndex, int slotIndex, Gear gear)
    {
        if (slotIndex >= 0 && slotIndex < GameManager.instance.gearManager.MaxGearSlotCount)
        {
            if (GearList[slotIndex].data != null) GearList[slotIndex].UnequipTo(cardIndex);
            GearList[slotIndex] = gear;
        }
    }
    public void ResetGear(int cardIndex, int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < GameManager.instance.gearManager.MaxGearSlotCount)
        {
            GearList[slotIndex].UnequipTo(cardIndex);
            GearList[slotIndex] = new(null);
        }
    }
    public void Initialize()
    {
        int max = GameManager.instance.gearManager.PhysicalMaxGearSlotCount;
        GearList = new Gear[max];
        for (int i = 0; i < max; i++) GearList[i] = new(null);
    }

    public Sprite GetIcon(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < GearList.Length)
        {
            return GearList[slotIndex].data.itemIcon;
        }
        else return GameManager.instance.gearManager.DefaultGearData.itemIcon;
    }
}