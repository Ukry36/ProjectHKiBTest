using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
public class InventoryView : MonoBehaviour
{
    public Transform panelParent;
    [SerializeField] private FilterPropertySO filterProperty;
    public UnityEvent<Item> OnPanelClicked;
    public InventoryViewModel viewModel;

    public void Start()
    {
        viewModel = new(GameManager.instance.inventoryManager);
        viewModel.RegistReactiveCommand((model) =>
        {
            UpdatePanels(model);
        }, this);
    }

    public virtual void UpdatePanels(InventoryManager inventoryManager)
    {
        List<Item> items = GameManager.instance.inventoryManager.playerInventory.Values.ToList();
        ItemPanel[] panels = panelParent.GetComponentsInChildren<ItemPanel>(true);

        if (items.Count > 0)
            for (int i = items.Count - 1; i >= 0; i--)
                if (!Filter(items[i].data))
                    items.RemoveAt(i);

        for (int i = 0; i < panels.Length; i++)
        {
            if (items.Count > i)
            {
                panels[i].gameObject.SetActive(true);
                panels[i].SetData(items[i]);
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
        viewModel.Execute();
    }

    public void ResetFilter()
    {
        filterProperty = null;
        viewModel.Execute();
    }

    public void ClickPanel(int index)
    {
        ItemPanel[] panels = panelParent.GetComponentsInChildren<ItemPanel>(true);
        OnPanelClicked.Invoke(panels[index].item);
        viewModel.Execute();
    }

    public void ChangePanel(Transform parent)
    {
        panelParent.gameObject.SetActive(false);
        panelParent = parent;
        panelParent.gameObject.SetActive(true);
        viewModel.Execute();
    }
}