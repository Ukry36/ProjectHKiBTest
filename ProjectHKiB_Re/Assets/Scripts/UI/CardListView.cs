using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using R3;

public class CardListView: MonoBehaviour
{
    public GearManagerViewModel viewModel;

    public GearManager gearManager;

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
                CardData card = cards.GetSafe(i); if (card == null) continue;
                if (card.mergedGearList == null || card.mergedGearList.Length < 1) view.patternView.Inititialize();
                view.patternView.SetPattern(card.mergedGearList[0].graffitiCodes[0]);
            }
        }
        SelectCard(_selectedNumber);
    }

    public void SelectCard(int num)
    {
        _selectedNumber = num;
        viewModel.SetCurrentEdittingCard(_selectedNumber);
    }
}