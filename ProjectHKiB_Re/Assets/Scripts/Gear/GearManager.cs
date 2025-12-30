using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class GearManager : MonoBehaviour
{
    [SerializeField] private GearMergeManagerSO gearMergeManager;
    public GearDataSO DefaultGearData { get => gearMergeManager.defaultGearData; }

    /*[HideInInspector]*/
    public List<CardData> playerCardEquipData;
    public int currentEquippedCardIndex;
    [field: SerializeField] public int PhysicalMaxGearSlotCount { get; private set; }
    [SerializeField] private int _maxGearSlotCount;
    public int MaxGearSlotCount
    {
        get => _maxGearSlotCount;
        set
        {
            _maxGearSlotCount = value;
            OnMaxSlotChanged?.Invoke();
        }
    }
    [field: SerializeField] public int PhysicalMaxCardCount { get; private set; }
    [SerializeField] private int _maxCardCount;
    public int MaxCardCount
    {
        get => _maxCardCount;
        set
        {
            _maxCardCount = value;
            OnMaxCardChanged?.Invoke();
        }
    }

    public bool canChangeCard = true;

    public Action OnMaxCardChanged;
    public Action OnMaxSlotChanged;
    public Action OnSetCardData;

    public void Start()
    {
        gearMergeManager.OnRealGearMade += FindObjectOfType<Player>(true).SetGear;
        playerCardEquipData = new(PhysicalMaxCardCount);
        for (int i = 0; i < PhysicalMaxCardCount; i++)
        {
            CardData data = new();
            playerCardEquipData.Add(data);
            data.Initialize();
        }
        SetMaxCard(1);
        SetMaxSlot(4); /////////////////////////////////////// temp!!!!!!!
        EquipCard(currentEquippedCardIndex);
        OnMaxCardChanged += () => EquipCard(currentEquippedCardIndex);
        OnMaxSlotChanged += () => EquipCard(currentEquippedCardIndex);
        OnSetCardData += () => EquipCard(currentEquippedCardIndex);
    }

    [Button]
    public void AddMaxCard()
    {
        SetMaxCard(MaxCardCount + 1);
    }

    public void SetMaxCard(int max)
    {
        MaxCardCount = max;
        if (MaxCardCount > PhysicalMaxCardCount) MaxCardCount = PhysicalMaxCardCount;
        if (MaxCardCount <= 0) MaxCardCount = 1;
    }

    [Button] public void AddMaxSlot() => SetMaxSlot(MaxGearSlotCount + 1);
    [Button] public void SubMaxSlot() => SetMaxSlot(MaxGearSlotCount - 1);
    public void SetMaxSlot(int max)
    {
        if (max < 0 || max > PhysicalMaxGearSlotCount) return;
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
        MaxGearSlotCount = max;
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
        if (cardIndex >= MaxCardCount || cardIndex < 0)
            return;
        if (gearSlotIndex >= MaxGearSlotCount || gearSlotIndex < 0)
            return;
        playerCardEquipData[cardIndex].ResetGear(cardIndex, gearSlotIndex);
        OnSetCardData?.Invoke();
    }

    public void SetGearData(int cardIndex, int gearSlotIndex, Gear gear)
    {
        if (cardIndex >= MaxCardCount || cardIndex < 0)
            return;
        if (gearSlotIndex >= MaxGearSlotCount || gearSlotIndex < 0)
            return;
        if (gearSlotIndex != gear.IsEquippedInCard(cardIndex) && gear.IsEquippedInCard(cardIndex) >= 0)
        {
            ResetGearData(cardIndex, gear.IsEquippedInCard(cardIndex));
        }

        gear.EquipTo(cardIndex, gearSlotIndex);
        playerCardEquipData[cardIndex].SetGear(cardIndex, gearSlotIndex, gear);
        OnSetCardData?.Invoke();
    }

    public void EquipCard(CardData data)
    {
        if (data == null) return;
        gearMergeManager.EquipMergedCard(data, MaxGearSlotCount);
    }

    public void EquipCard(int index)
    {
        currentEquippedCardIndex = index;
        EquipCard(playerCardEquipData[index]);
    }


    public void MergeGear(CardData data)
    {
        if (data == null) return;
        gearMergeManager.MergeCard(data, MaxGearSlotCount);
    }
}