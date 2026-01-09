using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class GearListView : AutoScroller
{
    public InventoryViewModel invenViewModel;
    public GearManagerViewModel gearViewModel;

    public InventoryManager inventoryManager;
    public GearManager gearManager;

    private Gear _currentSelectingGear;

    public void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        invenViewModel = new(inventoryManager);
        gearViewModel = new(gearManager);

        invenViewModel.GearList.Subscribe(list => UpdateList(list)).AddTo(this);
    }

    public void UpdateList(List<Gear> gearList)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject obj = transform.GetChild(i).gameObject;
            obj.SetActive(i < gearList.Count);
            if (i < gearList.Count)
            {
                ButtonEnhanced view = obj.GetComponent<ButtonEnhanced>();
                Gear gear = gearList[i]; 
                view.text.text = gear.data.name;
                view.number.text = ":1";
            }
        }
    }

    public void SelectGearToEquip(int index)
    {
        _currentSelectingGear = invenViewModel.GetGear(index);
    }

    public void SetGearData(int slotNum)
    {
        if (_currentSelectingGear != null)
            gearViewModel.SetGearData(slotNum, _currentSelectingGear);
    }

    
}