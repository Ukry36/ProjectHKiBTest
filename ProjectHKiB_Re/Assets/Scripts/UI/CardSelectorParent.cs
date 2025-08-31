using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardSelectorParent : MonoBehaviour, IPointerExitHandler
{
    public int max;
    public CardSelector topCard;
    public CardSelector bottomCard;
    public CardSelector[] cards;
    private List<CardSelector> activeCards;
    public Vector2 cardInterval;
    public Vector2 cardHighlightShift;
    public Vector2 cardHighlightPushShift;
    public Vector2 topCardChangeShift;
    public Vector2 bottomCardChangeShift;
    public float moveTime;
    public float delayTime;
    public bool interactable = true;

    public List<CardData> temp;

    private readonly List<Sequence> sequences = new();
    private void CompleteAllTweens()
    {
        for (int i = 0; i < sequences.Count; i++)
        {
            sequences[i].Complete();
        }
        sequences.Clear();
    }

    public void Awake()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].PointerClickEvent.AddListener(OnCardOfIndexClick);
            cards[i].PointerEnterEvent.AddListener(OnCardOfIndexEnter);
        }
        topCard.PointerEnterEvent.AddListener(OnTopCardEnter);
        UpdateCardDatas(temp);
        topCard.SetCardData(activeCards[0].cardData, 0);
    }

    public void SpreadCards(int index)
    {
        CompleteAllTweens();
        Sequence sequence = DOTween.Sequence();
        for (int i = 0; i < activeCards.Count; i++)
        {
            sequence.Insert(delayTime * (1 + index - i < 0 ? 0 : 1 + index - i),
                activeCards[i].transform.DOLocalMove(
                cardInterval * (activeCards.Count - i)
                + (i < index && index < activeCards.Count ? cardHighlightPushShift : Vector2.zero)
                + (i == index ? cardHighlightShift : Vector2.zero),
                moveTime));
        }
        sequence.Play();
        sequences.Add(sequence);
    }

    public void CollectCards()
    {
        CompleteAllTweens();
        Sequence sequence = DOTween.Sequence();
        for (int i = 0; i < activeCards.Count; i++)
        {
            sequence.Insert(delayTime * (activeCards.Count - i), activeCards[i].transform.DOLocalMove(Vector2.zero, moveTime));
        }
        sequence.Play();
        sequences.Add(sequence);
    }

    public void UpdateCardDatas(List<CardData> cardDatas)
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
        CollectCards();
        float delay = activeCards.Count * delayTime;
        SetInteractable(false);
        bottomCard.SetCardData(activeCards[topCardIndex].cardData, topCardIndex);
        Sequence sequence = DOTween.Sequence();
        sequence.Insert(delay, topCard.transform.DOLocalMove(topCardChangeShift, moveTime));
        sequence.Insert(delay, bottomCard.transform.DOLocalMove(bottomCardChangeShift, moveTime));
        delay += moveTime;
        sequence.InsertCallback(delay, () => bottomCard.SetCardData(topCard.cardData, topCardIndex));
        sequence.InsertCallback(delay, () => topCard.SetCardData(activeCards[topCardIndex].cardData, topCardIndex));
        sequence.Insert(delay, topCard.transform.DOLocalMove(bottomCardChangeShift, 0));
        sequence.Insert(delay, bottomCard.transform.DOLocalMove(topCardChangeShift, 0));
        delay += 0.0001f;
        sequence.Insert(delay, topCard.transform.DOLocalMove(Vector2.zero, moveTime));
        sequence.Insert(delay, bottomCard.transform.DOLocalMove(Vector2.zero, moveTime));
        sequence.OnComplete(() => SetInteractable(true));
        sequence.Play();
        sequences.Add(sequence);
    }

    public void SetInteractable(bool set)
    {
        interactable = set;
        if (set == false) CollectCards();
    }

    public void OnTopCardEnter(int temp)
    {
        if (interactable)
            SpreadCards(activeCards.Count);
    }
    public void OnCardOfIndexClick(int index)
    {
        if (interactable)
            ChangeTopCard(index);
    }

    public void OnCardOfIndexEnter(int index)
    {
        if (interactable)
            SpreadCards(index);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (interactable)
            CollectCards();
    }
}