using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class GearView : MonoBehaviour 
{
    public GraffitiPatternView[] patternView;
    public Image gearImage;
    public TextMeshProUGUI gearName;
    public TextMeshProUGUI description;

    public bool keepEmptyPattern;

    public void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        for (int i = 0; i < patternView.Length; i++)
        {
            patternView[i].Initialize();
            if (!keepEmptyPattern && i > 0)
                patternView[i].gameObject.SetActive(false);
        }
            
        
        if (gearImage) gearImage.enabled = false;
        if (gearName) gearName.text = null;
    }

    public void UpdateGearFromInventory(int gearIndex) => UpdateGear(GameManager.instance.inventoryManager.playerGearInventory.Values.ToList().GetSafe(gearIndex).data);
    public void UpdateGearFromCard(int slotNum) => UpdateGearFromCard(GameManager.instance.gearManager.CurrentEdittingCard, slotNum);
    public void UpdateGearFromCard(int cardIndex, int slotNum) => UpdateGear(GameManager.instance.gearManager.GetCardData(cardIndex).GearList.GetSafe(slotNum).data);

    public void UpdateGear(GearDataSO gear)
    {
        if (gear ==  null) 
        {
            Initialize();
            return;
        }
        for (int i = 0; i < patternView.Length; i++)
        {
            patternView[i].gameObject.SetActive(true);
            if (gear.graffitiCodes != null && i < gear.graffitiCodes.Count)
            {
                patternView[i].UpdatePattern(gear.graffitiCodes[i]);
            }
            else
            {
                if (!keepEmptyPattern && i > 0) patternView[i].gameObject.SetActive(false);
                else patternView[i].Initialize();
            }
        }

        if (gearImage)
        {
            gearImage.enabled = true;
            if (gear.itemIcon != null)
                gearImage.sprite = gear.itemIcon;
            else 
                gearImage.enabled = false;
        }
        
        if (gearName) gearName.text = gear.name;
        if (description) description.text = gear.description;
    }
}