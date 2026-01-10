using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GearView : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI gearName;
    public Image itemColor;
    public GraffitiPatternView[] patternView;

    public void SetData(Gear gear)
    {
        if (icon) icon.sprite = gear.data.itemIcon;
        if (gearName) gearName.text = gear.data.name;
        if (patternView != null)
        {
            for (int i = 0; i < patternView.Length; i++)
            {
                if (i < gear.data.graffitiCodes.Count) 
                    patternView[i].UpdatePattern(gear.data.graffitiCodes[i]);
                else 
                    patternView[i].Initialize();
            }
        }
        if (itemColor) 
        {
            itemColor.color = gear.data.graffitiCodes[0].color;
        }
    }
}