using UnityEngine;

public class CardListView: MonoBehaviour
{
    public CardView[] cards;

    private int CardCount => cards.Length;

    public void Initialize()
    {
        cards = GetComponentsInChildren<CardView>();
    }

    [NaughtyAttributes.Button]
    public void Up()
    {
       int currentCardNumber =  transform.GetChild(CardCount - 1).GetComponent<CardView>().number;
       if (currentCardNumber >= CardCount - 1) return;
       
       transform.GetChild(CardCount - currentCardNumber - 2).SetAsLastSibling();
    }

    [NaughtyAttributes.Button]
    public void Down()
    {
        int currentCardNumber =  transform.GetChild(CardCount - 1).GetComponent<CardView>().number;
       if (currentCardNumber <= 0) return;

       transform.GetChild(CardCount - 1).SetSiblingIndex(CardCount - currentCardNumber - 1);
    }
}