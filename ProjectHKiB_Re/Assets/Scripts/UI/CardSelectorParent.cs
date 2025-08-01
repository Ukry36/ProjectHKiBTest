using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardSelectorParent : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
{
    public int max;
    public CardSelector topCard;
    public CardSelector[] cards;
    private List<CardSelector> activeCards;
    public Vector2 cardInterval;
    public float cardHighlightInterval;
    public float cardHighlightHeight;
    private bool spread;

    public List<CardData> temp;

    public void Start()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].PointerClickEvent.AddListener(OnCardOfIndexClick);
            cards[i].PointerEnterEvent.AddListener(OnCardOfIndexEnter);
            cards[i].PointerExitEvent.AddListener(OnCardOfIndexExit);
        }
        UpdateCards(temp);
    }

    public void SpreadCards()
    {
        spread = true;
        for (int i = activeCards.Count; i >= 0; i--)
        {
            activeCards[i].transform.DOLocalMove(cardInterval * (activeCards.Count - i + 1), 0.1f);
        }
    }

    public void CollectCards()
    {
        spread = false;
        for (int i = activeCards.Count; i >= 0; i--)
        {
            activeCards[i].transform.DOLocalMove(Vector2.zero, 0.1f);
        }
    }

    public void UpdateCards(List<CardData> cardDatas)
    {
        cardDatas ??= new() { new() };
        max = cardDatas.Count;
        if (max > cards.Length) max = cards.Length;
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].gameObject.SetActive(false);
        }
        activeCards = new(max);
        for (int i = 0; i < max; i++)
        {
            cards[i].SetCardData(cardDatas[i], i);
            cards[i].gameObject.SetActive(true);
            activeCards.Add(cards[i]);
        }
    }

    public void ChangeTopCard(int topCardIndex)
    {
        if (topCardIndex >= activeCards.Count) topCardIndex = activeCards.Count - 1;
        if (topCardIndex < 0) topCardIndex = 0;
        topCard.SetCardData(activeCards[topCardIndex].cardData, topCardIndex);
    }

    public void OnCardOfIndexClick(int index)
    {
        ChangeTopCard(index);
        //anim
    }

    public void OnCardOfIndexEnter(int index)
    {
        if (!spread)
            SpreadCards();
        //anim
    }

    public void OnCardOfIndexExit(int index)
    {
        if (spread)
            CollectCards();
        //anim
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //open card maker
    }

    public void OnPointerEnter(PointerEventData eventData)
    {

    }

    public void OnPointerExit(PointerEventData eventData)
    {

    }


}