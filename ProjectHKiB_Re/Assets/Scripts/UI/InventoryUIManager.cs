using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class InventoryUIManager : MonoBehaviour
{
    public Transform panelParent;
    [SerializeField] private FilterPropertySO filterProperty;
    public UnityEvent<Item> OnPanelClicked;

    public virtual void UpdatePanels()
    {
        List<Item> items = GameManager.instance.databaseManager.playerInventory.Values.ToList();
        ItemPanel[] panels = panelParent.GetComponentsInChildren<ItemPanel>(true);

        if (items.Count > 0)
            for (int i = items.Count - 1; i >= 0; i--)
                if (!Filter(items[i].data))
                    items.RemoveAt(i);

        for (int i = 0; i < panels.Length; i++)
        {
            if (items.Count > i)
            {
                Item item = items[i];
                panels[i].gameObject.SetActive(true);
                panels[i].SetData(item);
            }
            else
            {
                panels[i].gameObject.SetActive(false);
            }
        }
    }

    public bool Filter(ItemDataSO item)
    {
        if (filterProperty == null) return true;
        return item.parentProperties.Contains(filterProperty);
    }

    public void SetFilter(FilterPropertySO filter)
    {
        filterProperty = filter;
        UpdatePanels();
    }

    public void ResetFilter()
    {
        filterProperty = null;
        UpdatePanels();
    }

    public void ClickPanel(int index)
    {
        List<Item> items = GameManager.instance.databaseManager.playerInventory.Values.ToList();
        if (items.Count > index)
        {
            OnPanelClicked.Invoke(items[index]);
        }
        UpdatePanels();
    }

    public void ChangePanel(Transform parent)
    {
        panelParent.gameObject.SetActive(false);
        panelParent = parent;
        panelParent.gameObject.SetActive(true);
        UpdatePanels();
    }
}