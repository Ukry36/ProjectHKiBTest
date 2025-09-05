using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class Item
{
    public readonly ItemDataSO data;
    public int ID { get => data.GetInstanceID(); }
    public StateMachineSO ItemEvent { get => data.itemUseEvent; }
    public int Count { get; private set; }

    public Item(ItemDataSO data, int count = 1)
    {
        if (count <= 0 || !data.type.canStack) count = 1;
        this.data = data;
        Count = count;
    }

    public bool ItemCountCheck(int count) => Count - count >= 0;
    public void UnstackItem(int count)
    {
        Count -= count;
        if (Count < 0) Count = 0;
    }

    public void StackItem(int count)
    {
        if (data.type.canStack)
            Count += count;
        else
            Count = 1;
    }
}
public class DatabaseManager : StateController
{
    public Dictionary<int, Item> playerInventory;
    public List<CardData> playerCardEquipData;
    public CustomVariableSets parameters;
    public Vector3 playerPos;
    public Scene scene;

    public void AddItem(ItemDataSO item, int count)
    {
        if (!item) return;
        int ID = item.GetInstanceID();
        if (playerInventory.ContainsKey(ID))
            playerInventory[ID].StackItem(count);
        else
            playerInventory[ID] = new(item, count);
    }

    public Item GetInventoryItem(int ID)
    {
        if (!playerInventory.ContainsKey(ID)) return null;
        return playerInventory[ID];
    }

    public bool UseInventoryItem(int ID, int count)
    {
        if (!playerInventory.ContainsKey(ID) || playerInventory[ID].ItemCountCheck(count))
            return false;
        playerInventory[ID].UnstackItem(count);
        Initialize(playerInventory[ID].ItemEvent);
        return true;
    }

    public void RemoveInventoryItem(int ID, int count)
    {
        if (!playerInventory.ContainsKey(ID)) return;
        playerInventory[ID].UnstackItem(count);
    }

    public CardData GetCardData(int index)
    {
        if (index >= playerCardEquipData.Count)
            return null;
        return playerCardEquipData[index];
    }

    public void SetCardData(int index, CardData data)
    {
        if (index >= playerCardEquipData.Count)
            return;
        playerCardEquipData[index] = data;
    }
}