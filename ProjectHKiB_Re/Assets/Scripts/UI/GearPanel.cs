using TMPro;
using UnityEngine;
using UnityEngine.Events;
public class GearPanel : ItemPanel
{
    public int gearID;
    public CardSelectorParent cardSelectorParent;
    public void SetGearData(GearDataSO data)
    {
        gearID = data.GetInstanceID();
        if (icon9x9) icon9x9.sprite = data.itemIcon9x9;
        if (icon5x5 && data.parentProperties != null && data.parentProperties.Length > 0)
            icon5x5.sprite = data.parentProperties[0].icon5x5;
        if (itemName) itemName.text = data.name;
        if (itemColor) itemColor.color = data.color;
        if (itemTooltip) itemTooltip.SetData(data);
        Debug.Log(gearID);
    }
    public void ApplyGear()
    {
        Debug.Log(gearID);
        Debug.Log(icon5x5);
        cardSelectorParent.SetGearData(gearID);
    }
    [NaughtyAttributes.Button]
    public void De()
        => Debug.Log(gearID);
}