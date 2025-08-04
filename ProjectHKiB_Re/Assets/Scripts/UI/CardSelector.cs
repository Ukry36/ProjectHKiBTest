using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardSelector : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    public int index;
    public bool highlighted;
    public CardData cardData;
    public Image[] gearIcons;

    public UnityEvent<int> PointerClickEvent;
    public UnityEvent<int> PointerEnterEvent;

    public void Start()
    {
        if (gearIcons == null || gearIcons.Length == 0) Debug.LogError("No GearIcons");
        for (int i = 0; i < gearIcons.Length; i++)
        {
            if (gearIcons[i] == null) Debug.LogError("GearIcon [" + i + "] is null");
        }
    }

    public void SetCardData(CardData cardData, int index)
    {
        this.index = index;
        this.cardData = cardData;
        if (cardData == null)
        {
            for (int i = 0; i < gearIcons.Length; i++) gearIcons[i].gameObject.SetActive(false);
            return;
        }
        for (int i = 0; i < gearIcons.Length; i++)
        {
            if (i < cardData.gearList.Count && cardData.gearList[i] != null)
            {
                gearIcons[i].gameObject.SetActive(true);
                gearIcons[i].sprite = cardData.gearList[i].itemIcon;
            }
            else
            {
                gearIcons[i].gameObject.SetActive(false);
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

    public void OnDestroy()
    {
        PointerClickEvent.RemoveAllListeners();
        PointerEnterEvent.RemoveAllListeners();
    }
}