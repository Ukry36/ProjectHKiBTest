using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;

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

        viewModel.RegistReactiveCommand(model =>
        {
            UpdateCards(model);
        }, this);
    }

    [NaughtyAttributes.Button] public void UpdateCards() => UpdateCards(gearManager);
    public void UpdateCards(GearManager gearManager)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject obj = transform.GetChild(i).gameObject;
            obj.SetActive(i < gearManager.MaxCardCount);
            if (i < gearManager.MaxCardCount)
            {
                CardView view = obj.GetComponent<CardView>();
                CardData card = gearManager.GetCardData(i); if (card == null) continue;
                if (card.mergedGearList == null || card.mergedGearList.Length < 1) view.patternView.Inititialize();
                view.patternView.SetPattern(card.mergedGearList[0].graffitiCodes[0]);
            }
        }
        gearManager.currentEditingCardNum = _selectedNumber;
    }

    public void SelectCard(int num)
    {
        _selectedNumber = num;
        viewModel.Execute();
    }
}