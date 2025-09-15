using System.Collections.Generic;
using System.Linq;
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
    public Image[] disabledSlots;

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

    public void SetSlotCount(int max)
    {
        for (int i = 0; i < disabledSlots.Length; i++)
        {
            if (i < max) disabledSlots[i].gameObject.SetActive(false);
            else disabledSlots[i].gameObject.SetActive(true);
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
            gearIcons[i].sprite = cardData.GetMergedIcon(i);
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