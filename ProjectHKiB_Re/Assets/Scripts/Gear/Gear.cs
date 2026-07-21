using System;
using System.Collections.Generic;

[Serializable]
public class Gear
{
    public readonly GearDataSO data;
    public int ID { get => data.GetInstanceID(); }
    public StateMachineSO ItemEvent { get => data.itemUseEvent; }
    public List<int> equippedCards;
    public int slot;

    public Gear(GearDataSO data)
    {
        this.data = data;
        equippedCards = new();
    }

    public int IsEquippedInCard(int cardIndex)
    {
        if (equippedCards.Contains(cardIndex))
            return slot;
        else
            return -1;
    }

    public void EquipTo(int cardIndex, int slotIndex)
    {
        if (equippedCards.Contains(cardIndex)) return;
        equippedCards.Add(cardIndex);
        slot = slotIndex;
    }
    public void UnequipTo(int cardIndex)
    {
        if (equippedCards.Contains(cardIndex))
            equippedCards.Remove(cardIndex);
        slot = -1;
    }
}