using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardSelector : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public int index;
    public bool onTop;
    public CardData cardData;
    public Image[] gearIcons;
    public CardSelectorParent cardSelectorParent;

    public UnityEvent<int> PointerClickEvent;
    public UnityEvent<int> PointerEnterEvent;
    public UnityEvent<int> PointerExitEvent;

    public void SetCardData(CardData cardData, int index)
    {
        this.index = index;
        this.cardData = cardData;
        if (gearIcons == null) return;
        if (cardData == null)
        {
            for (int i = 0; i < gearIcons.Length; i++) if (gearIcons[i] != null) gearIcons[i].sprite = null;
            return;
        }
        if (cardData.gearList == null)
        {
            for (int i = 0; i < gearIcons.Length; i++) if (gearIcons[i] != null) gearIcons[i].sprite = null;
            return;
        }
        for (int i = 0; i < cardData.gearList.Count; i++)
        {
            if (i >= gearIcons.Length) break;
            if (cardData.gearList[i] != null && gearIcons[i] != null)
            {
                gearIcons[i].sprite = cardData.gearList[i].itemIcon;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PointerClickEvent.Invoke(index);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEnterEvent.Invoke(index);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PointerExitEvent.Invoke(index);
    }

    public void OnDestroy()
    {
        PointerClickEvent.RemoveAllListeners();
        PointerEnterEvent.RemoveAllListeners();
        PointerExitEvent.RemoveAllListeners();
    }
}