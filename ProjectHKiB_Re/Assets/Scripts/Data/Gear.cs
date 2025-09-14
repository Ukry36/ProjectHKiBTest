using System.Collections.Generic;

public class Gear
{
    public readonly GearDataSO data;
    public int ID { get => data.GetInstanceID(); }
    public StateMachineSO ItemEvent { get => data.itemUseEvent; }
    public List<int> equippedCards;

    public Gear(GearDataSO data)
    {
        this.data = data;
        equippedCards = new();
    }

    public bool IsEquippedInCard(int cardIndex) => equippedCards.Contains(cardIndex);

    public void EquipTo(int cardIndex)
    {
        if (equippedCards.Contains(cardIndex)) return;
        equippedCards.Add(cardIndex);
    }
    public void UnequipTo(int cardIndex)
    {
        if (equippedCards.Contains(cardIndex))
            equippedCards.Remove(cardIndex);
    }
}