using System.Collections.Generic;
using System.Linq;

public class GearInventoryUIManager : InventoryUIManager
{
    public override void UpdatePanels()
    {
        List<GearDataSO> gears = GameManager.instance.databaseManager.playerGearInventory.Values.ToList();
        GearPanel[] panels = panelParent.GetComponentsInChildren<GearPanel>(true);

        if (gears.Count > 0)
            for (int i = gears.Count - 1; i >= 0; i--)
                if (!Filter(gears[i]))
                    gears.RemoveAt(i);

        for (int i = 0; i < panels.Length; i++)
        {
            if (gears.Count > i)
            {
                GearDataSO gear = gears[i];
                panels[i].gameObject.SetActive(true);
                panels[i].SetGearData(gear);
            }
            else
            {
                panels[i].gameObject.SetActive(false);
            }
        }
    }

}