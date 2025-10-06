using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class GearInventoryView : MonoBehaviour
{
    public Transform panelParent;
    [SerializeField] private FilterPropertySO filterProperty;
    public CardSelectorParent cardSelectorForEdit;

    public GearInventoryViewModel viewModel;

    public void Start()
    {
        viewModel = new(GameManager.instance.inventoryManager);
        viewModel.RegistReactiveCommand((model) =>
        {
            UpdatePanels(model);
        }, this);
    }
    public void UpdatePanels(InventoryManager inventoryManager)
    {
        List<Gear> gears = inventoryManager.playerGearInventory.Values.ToList();
        GearPanel[] panels = panelParent.GetComponentsInChildren<GearPanel>(true);

        if (gears.Count > 0)
            for (int i = gears.Count - 1; i >= 0; i--)
                if (!Filter(gears[i].data))
                    gears.RemoveAt(i);

        for (int i = 0; i < panels.Length; i++)
        {
            if (gears.Count > i)
            {
                panels[i].gameObject.SetActive(true);
                panels[i].SetData(gears[i]);
                panels[i].Highlight(cardSelectorForEdit.topCard.index);
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
        GearPanel[] panels = panelParent.GetComponentsInChildren<GearPanel>(true);
        cardSelectorForEdit.SetGearData(panels[index].gear);
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