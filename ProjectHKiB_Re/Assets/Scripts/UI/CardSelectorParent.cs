using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class CardSelectorParent : MonoBehaviour, IPointerExitHandler
{
    public CardSelector topCard;
    public CardSelector bottomCard;
    public List<CardSelector> cards;
    public Transform cardsParent;
    public Vector2 cardInterval;
    public Vector2 cardHighlightShift;
    public Vector2 cardHighlightPushShift;
    public Vector2 topCardChangeShift;
    public Vector2 bottomCardChangeShift;
    public float moveTime;
    public float delayTime;
    public bool interactable = true;
    public int currentSlot;

    public UnityEvent OnTopCardChange;
    public void SetCurrentSlot(int set) => currentSlot = set;

    private readonly List<Sequence> sequences = new();
    private void CompleteAllTweens()
    {
        for (int i = 0; i < sequences.Count; i++)
        {
            sequences[i].Complete();
        }
        sequences.Clear();
    }

    public void Initialize()
    {
        cards = new(GameManager.instance.gearManager.physicalMaxCardCount);
        CardSelector[] allCards = cardsParent.GetComponentsInChildren<CardSelector>(true);
        for (int i = 0; i < allCards.Length; i++)
        {
            allCards[i].PointerClickEvent.AddListener(OnCardOfIndexClick);
            allCards[i].PointerEnterEvent.AddListener(OnCardOfIndexEnter);
            allCards[i].cardData.Initialize();
        }
        bottomCard.cardData.Initialize();
        topCard.cardData.Initialize();
        UpdateCardDatas();
        topCard.PointerEnterEvent.AddListener(OnTopCardEnter);
        topCard.SetCardData(allCards[0].cardData, 0);
        GameManager.instance.gearManager.OnSetCardData.AddListener(UpdateCardDatas);
    }

    public void SpreadCards(int index)
    {
        CompleteAllTweens();
        Sequence sequence = DOTween.Sequence();
        for (int i = 0; i < cards.Count; i++)
        {
            sequence.Insert(delayTime * (1 + index - i < 0 ? 0 : 1 + index - i),
                cards[i].transform.DOLocalMove(
                cardInterval * (cards.Count - i)
                + (i < index && index < cards.Count ? cardHighlightPushShift : Vector2.zero)
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
        for (int i = 0; i < cards.Count; i++)
        {
            sequence.Insert(delayTime * (cards.Count - i), cards[i].transform.DOLocalMove(Vector2.zero, moveTime));
        }
        sequence.Play();
        sequences.Add(sequence);
    }

    public void UpdateCardDatas()
    {
        List<CardData> cardDatas = GameManager.instance.gearManager.playerCardEquipData;
        CardSelector[] allCards = cardsParent.GetComponentsInChildren<CardSelector>(true);
        cards.Clear();
        int max = cardDatas.Count;
        for (int i = 0; i < allCards.Length; i++)
        {
            allCards[i].gameObject.SetActive(false);
            allCards[i].SetSlotCount(GameManager.instance.gearManager.maxGearSlotCount);
            if (i < max)
            {
                allCards[i].SetCardData(cardDatas[i], i);
                allCards[i].gameObject.SetActive(true);
                cards.Add(allCards[i]);
            }
        }
        bottomCard.SetSlotCount(GameManager.instance.gearManager.maxGearSlotCount);
        topCard.SetSlotCount(GameManager.instance.gearManager.maxGearSlotCount);
        topCard.SetCardData(cards[topCard.index].cardData, topCard.index);
    }

    public void ChangeTopCard(int index)
    {
        if (index == topCard.index) return;
        if (index >= cards.Count) index = cards.Count - 1;
        if (index < 0) index = 0;
        float delay = cards.Count * delayTime;
        SetInteractable(false);
        bottomCard.SetCardData(cards[index].cardData, index);
        Sequence sequence = DOTween.Sequence();
        sequence.Insert(delay, topCard.transform.DOLocalMove(topCardChangeShift, moveTime));
        sequence.Insert(delay, bottomCard.transform.DOLocalMove(bottomCardChangeShift, moveTime));
        delay += moveTime;
        sequence.InsertCallback(delay, () => bottomCard.SetCardData(topCard.cardData, index));
        sequence.InsertCallback(delay, () => topCard.SetCardData(cards[index].cardData, index));
        sequence.Insert(delay, topCard.transform.DOLocalMove(bottomCardChangeShift, 0));
        sequence.Insert(delay, bottomCard.transform.DOLocalMove(topCardChangeShift, 0));
        delay += 0.0001f;
        sequence.Insert(delay, topCard.transform.DOLocalMove(Vector2.zero, moveTime));
        sequence.Insert(delay, bottomCard.transform.DOLocalMove(Vector2.zero, moveTime));
        sequence.OnComplete(() => { SetInteractable(true); OnTopCardChange?.Invoke(); });
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
            SpreadCards(cards.Count);
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