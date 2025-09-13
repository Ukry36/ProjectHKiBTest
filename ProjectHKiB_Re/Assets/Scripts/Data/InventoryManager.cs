using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class InventoryManager : MonoBehaviour
{
    public Dictionary<int, Item> playerInventory = new();
    public Dictionary<int, Gear> playerGearInventory = new();

    void Start() // temp
    {
        ItemDataSO[] items = Resources.LoadAll<ItemDataSO>("Items");
        foreach (ItemDataSO data in items)
        {
            Debug.Log(data.name);
            AddItem(data, 99);
        }
        GearDataSO[] gears = Resources.LoadAll<GearDataSO>("Items/Gears");
        foreach (GearDataSO data in gears)
        {
            Debug.Log(data.name);
            AddItem(data, 99);
            AddGear(data);
        }
    }

    public void AddItem(ItemDataSO item, int count)
    {
        if (!item) return;
        int ID = item.GetInstanceID();
        if (playerInventory.ContainsKey(ID))
            playerInventory[ID].StackItem(count);
        else
            playerInventory[ID] = new(item, count);
    }
    public void AddGear(GearDataSO data) => playerGearInventory[data.GetInstanceID()] = new(data);

    public Item GetItem(int ID)
    {
        if (!playerInventory.ContainsKey(ID)) return null;
        return playerInventory[ID];
    }

    public Item GetItemByIndex(int index)
    {
        Item[] items = playerInventory.Values.ToArray();
        if (items.Length > index)
            return items[index];
        else return null; // or defaultItem
    }

    public bool UseInventoryItem(int ID, int count)
    {
        if (!playerInventory.ContainsKey(ID) || playerInventory[ID].ItemCountCheck(count))
            return false;
        playerInventory[ID].UnstackItem(count);
        //Initialize(playerInventory[ID].ItemEvent); // play event
        return true;
    }

    public void RemoveInventoryItem(int ID, int count)
    {
        if (!playerInventory.ContainsKey(ID)) return;
        playerInventory[ID].UnstackItem(count);
    }
}