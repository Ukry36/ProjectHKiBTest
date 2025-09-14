using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class GearInventoryUIManager : MonoBehaviour
{
    public Transform panelParent;
    [SerializeField] private FilterPropertySO filterProperty;
    public UnityEvent<Gear> OnPanelClicked;
    public CardSelectorParent cardSelectorForEdit;

    public void UpdatePanels()
    {
        List<Gear> gears = GameManager.instance.inventoryManager.playerGearInventory.Values.ToList();
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
        UpdatePanels();
    }

    public void ResetFilter()
    {
        filterProperty = null;
        UpdatePanels();
    }

    public void ClickPanel(int index)
    {
        GearPanel[] panels = panelParent.GetComponentsInChildren<GearPanel>(true);
        OnPanelClicked?.Invoke(panels[index].gear);
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