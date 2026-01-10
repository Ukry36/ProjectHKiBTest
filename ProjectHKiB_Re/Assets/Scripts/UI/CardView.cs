using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour 
{
    public GraffitiPatternView[] patternView;
    public Image[] gearImage;

    public void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        for (int i = 0; i < patternView.Length; i++) 
            patternView[i].Initialize();
        
        for (int i = 0; i < gearImage.Length; i++)
            gearImage[i].enabled = false;
    }

    public void UpdateCard(int cardIndex) => UpdateCard(GameManager.instance.gearManager.GetCardData(cardIndex));

    public void UpdateCard(CardData card)
    {
        if (card ==  null) Initialize();
        
        for (int i = 0; i < patternView.Length; i++)
        {
            if (i < card.mergedGearList[0].graffitiCodes.Count)
                patternView[i].UpdatePattern(card.mergedGearList[0].graffitiCodes[i]);
            else
                patternView[i].Initialize();
        }
        
        for (int i = 0; i < gearImage.Length; i++)
        {
            gearImage[i].enabled = true;
            if (i < card.mergedGearList.Length && gearImage[i] != null && card.mergedGearList[i].itemIcon != null)
            {
                gearImage[i].sprite = card.mergedGearList[i].itemIcon;
            }
            else 
                gearImage[i].enabled = false;
        }
    }
}