using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CardData
{
    public string cardName;
    public List<int> mergeInfo;
    public List<GearDataSO> mergedGearList;
    public List<Gear> gearList;
    public void SetGear(int slotIndex, Gear gear)
    {
        if (slotIndex >= 0 && slotIndex < GameManager.instance.gearManager.maxGearSlotCount)
        {
            gearList[slotIndex] = gear;
            MergeGear();
        }
    }
    public void Initialize() => MergeGear();

    public void MergeGear()
    {
        GameManager.instance.gearManager.gearMergeManager.MergeCard(this);
    }

    public Sprite GetMergedIcon(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < mergedGearList.Count)
        {
            return mergedGearList[slotIndex].itemIcon9x9;
        }
        else return GameManager.instance.gearManager.gearMergeManager.defaultGearData.itemIcon9x9;
    }

    public Sprite GetIcon(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < mergedGearList.Count)
        {
            return gearList[slotIndex].data.itemIcon9x9;
        }
        else return GameManager.instance.gearManager.gearMergeManager.defaultGearData.itemIcon9x9;
    }
}