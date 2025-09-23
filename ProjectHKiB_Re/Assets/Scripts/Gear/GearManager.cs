using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GearManager : MonoBehaviour
{
    public GearMergeManagerSO gearMergeManager;
    [SerializeField] private CardData card;
    [SerializeField] private Player player;

    public List<CardData> playerCardEquipData;
    public int currentEquippedCardIndex;
    public int maxGearSlotCount;
    public bool canChangeCard = true;

    public CardSelectorParent selectorForEdit;
    public CardSelectorParent selectorForEquip;


    public UnityEvent OnSetCardData;

    public void Start()
    {
        gearMergeManager.OnRealGearMade += player.SetGear;
        selectorForEquip.topCard.cardData.Initialize();
        EquipCard(selectorForEquip.topCard.cardData);
    }

    [NaughtyAttributes.Button]
    public void EquipTest()
    {
        card.MergeGear();
        gearMergeManager.EquipMergedCard(card);
    }

    public void OnDestroy()
    {
        gearMergeManager.OnRealGearMade -= player.SetGear;
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
        Gear gear = playerCardEquipData[cardIndex].gearList[gearSlotIndex];
        if (gear != null)
        {
            playerCardEquipData[cardIndex].gearList.Remove(gear);
            gear.UnequipTo(cardIndex);
        }
    }

    public void SetGearData(int cardIndex, int gearSlotIndex, Gear gear)
    {
        if (cardIndex >= playerCardEquipData.Count || cardIndex < 0)
            return;
        if (gearSlotIndex >= maxGearSlotCount || gearSlotIndex < 0)
            return;
        if (gear.IsEquippedInCard(cardIndex))
            return;
        gear.EquipTo(cardIndex);
        playerCardEquipData[cardIndex].SetGear(gearSlotIndex, gear);
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
        gearMergeManager.EquipMergedCard(data);
    }

    public void EquipCardBySelector()
    {
        if (currentEquippedCardIndex == selectorForEquip.topCard.index) return;
        currentEquippedCardIndex = selectorForEquip.topCard.index;
        EquipCard(selectorForEquip.topCard.cardData);
    }
}