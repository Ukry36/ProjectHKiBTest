using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryUIManager : MonoBehaviour
{
    public ItemPanel[] panels;

    //public ItemFilterSO

    public void UpdatePanels()
    {
        List<Item> items = GameManager.instance.databaseManager.playerInventory.Values.ToList();
        for (int i = 0; i < panels.Length; i++)
        {
            if (items.Count >= i)
            {
                Item item = items[i];
                panels[i].SetData(item.data.itemIcon9x9, item.data.name, item.data.color);
                panels[i].gameObject.SetActive(true);
            }
            else
            {
                panels[i].gameObject.SetActive(false);
            }
        }
    }
}