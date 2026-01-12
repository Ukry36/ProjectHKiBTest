using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using R3;

public class CardListView: MonoBehaviour
{
    public GearManagerViewModel viewModel;

    public GearManager gearManager;

    public CardView cardSlotPreview;

    public void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        viewModel = new(gearManager);

        viewModel.CardList.Subscribe(list => UpdateList(list)).AddTo(this);
        //viewModel.CurrentCard.Subscribe(num => SelectCard(num)).AddTo(this);
    }

    public void UpdateList(List<CardData> cards)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject obj = transform.GetChild(i).gameObject;
            obj.SetActive(i < cards.Count);
            if (i < cards.Count)
            {
                CardView view = obj.GetComponent<CardView>();
                view.UpdateCard(i);
            }
        }
        SelectCard(viewModel.CurrentCard.CurrentValue);
    }

    public void SelectCard(int num)
    {
        if(viewModel == null) return;
        
        viewModel.SetCurrentEdittingCard(num);
        if(cardSlotPreview)
        {
            cardSlotPreview.transform.position = transform.GetChild(num).position;
            cardSlotPreview.UpdateCard(num);
        }
    }
}