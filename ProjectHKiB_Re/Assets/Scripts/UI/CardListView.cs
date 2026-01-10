using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using R3;

public class CardListView: MonoBehaviour
{
    public GearManagerViewModel viewModel;

    public GearManager gearManager;

    public CardView cardPreview;
    public CardView cardSlotPreview;

    private int _selectedNumber;

    public void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        viewModel = new(gearManager);

        viewModel.CardList.Subscribe(list => UpdateList(list)).AddTo(this);
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
        SelectCard(_selectedNumber);
    }

    public void SelectCard(int num)
    {
        _selectedNumber = num;
        if(viewModel == null) return;
        
        viewModel.SetCurrentEdittingCard(_selectedNumber);
        if(cardPreview)
        {
            cardPreview.transform.position = transform.GetChild(_selectedNumber).position;
            cardPreview.UpdateCard(_selectedNumber);
        }
        if(cardSlotPreview)
        {
            cardSlotPreview.transform.position = transform.GetChild(_selectedNumber).position;
            cardSlotPreview.UpdateCard(_selectedNumber);
        }
    }
}