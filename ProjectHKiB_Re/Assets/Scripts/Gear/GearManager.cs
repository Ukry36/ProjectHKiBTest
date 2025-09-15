using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

public class GearManager : MonoBehaviour
{
    [SerializeField] private GearMergeManagerSO gearMergeManager;
    public GearDataSO DefaultGearData { get => gearMergeManager.defaultGearData; }

    /*[HideInInspector]*/
    public List<CardData> playerCardEquipData;
    public int currentEquippedCardIndex;
    public int physicalMaxGearSlotCount;
    public int maxGearSlotCount;
    public int physicalMaxCardCount;
    public bool canChangeCard = true;

    public CardSelectorParent selectorForEdit;
    public CardSelectorParent selectorForEquip;


    public UnityEvent OnSetCardData;

    public void Start()
    {
        gearMergeManager.OnRealGearMade += FindObjectOfType<Player>(true).SetGear;
        playerCardEquipData = new();
        AddMaxCard();
        SetMaxSlot(4); /////////////////////////////////////// temp!!!!!!!
        selectorForEdit.Initialize();
        selectorForEquip.Initialize();
        EquipCard(selectorForEquip.topCard.cardData);
    }

    public void OnDestroy()
    {
        //gearMergeManager.OnRealGearMade -= FindObjectOfType<Player>(true).SetGear;
    }

    [Button]
    public void AddMaxCard()
    {
        if (playerCardEquipData.Count < physicalMaxCardCount)
        {
            CardData data = new();
            playerCardEquipData.Add(data);
            data.Initialize();
            selectorForEdit.UpdateCardDatas();
            selectorForEquip.UpdateCardDatas();
        }

    }
    [Button] public void AddMaxSlot() => SetMaxSlot(maxGearSlotCount + 1);
    [Button] public void SubMaxSlot() => SetMaxSlot(maxGearSlotCount - 1);
    public void SetMaxSlot(int max)
    {
        if (max < 0 || max > physicalMaxGearSlotCount) return;
        for (int i = 0; i < playerCardEquipData.Count; i++)
        {
            for (int j = 0; j < playerCardEquipData[i].gearList.Length; j++)
            {
                if (j >= max)
                {
                    playerCardEquipData[i].ResetGear(i, j);
                }
            }
        }
        maxGearSlotCount = max;
        selectorForEdit.UpdateCardDatas();
        selectorForEquip.UpdateCardDatas();
        EquipCardBySelector();
    }

    public CardData GetCardData(int index)
    {
        if (index >= playerCardEquipData.Count || index < 0)
            return null;
        return playerCardEquipData[index];
    }

    public void SetCardData(int cardIndex, CardData data)
    {
        if (cardIndex >= playerCardEquipData.Count || cardIndex < 0)
            return;
        playerCardEquipData[cardIndex] = data;
        OnSetCardData?.Invoke();
    }

    public void SetEquippedCardData(CardData data)
    {
        SetCardData(currentEquippedCardIndex, data);
    }

    public void ResetGearData(int cardIndex, int gearSlotIndex)
    {
        if (cardIndex >= playerCardEquipData.Count || cardIndex < 0)
            return;
        if (gearSlotIndex >= maxGearSlotCount || gearSlotIndex < 0)
            return;
        playerCardEquipData[cardIndex].ResetGear(cardIndex, gearSlotIndex);
    }

    public void SetGearData(int cardIndex, int gearSlotIndex, Gear gear)
    {
        if (cardIndex >= playerCardEquipData.Count || cardIndex < 0)
            return;
        if (gearSlotIndex >= maxGearSlotCount || gearSlotIndex < 0)
            return;
        if (gearSlotIndex != gear.IsEquippedInCard(cardIndex) && gear.IsEquippedInCard(cardIndex) >= 0)
        {
            ResetGearData(cardIndex, gear.IsEquippedInCard(cardIndex));
        }

        gear.EquipTo(cardIndex, gearSlotIndex);
        playerCardEquipData[cardIndex].SetGear(cardIndex, gearSlotIndex, gear);
        OnSetCardData?.Invoke();
    }

    public void SetGearDataBySelector(Gear gear)
    {
        SetGearData(selectorForEdit.topCard.index, selectorForEdit.currentSlot, gear);
        if (selectorForEdit.topCard.index == currentEquippedCardIndex)
            EquipCard(selectorForEdit.topCard.cardData);
    }

    public void EquipCard(CardData data)
    {
        if (data == null) return;
        gearMergeManager.EquipMergedCard(data, maxGearSlotCount);
    }

    public void EquipCardBySelector()
    {
        if (currentEquippedCardIndex == selectorForEquip.topCard.index) return;
        currentEquippedCardIndex = selectorForEquip.topCard.index;
        EquipCard(selectorForEquip.topCard.cardData);
    }

    public void MergeGear(CardData data)
    {
        if (data == null) return;
        gearMergeManager.MergeCard(data, maxGearSlotCount);
    }
}