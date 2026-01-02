using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;

public class CardListView: MonoBehaviour
{

    public GearManagerViewModel viewModel;

    public GearManager gearManager;

    private int CardAssetCount => transform.childCount;
    private int GetCardNum(int i) => transform.GetChild(i).GetComponent<CardView>().cardNum;
    private int CurrentCardNum => GetCardNum(CardAssetCount - 1);

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
        while (CurrentCardNum >= gearManager.MaxCardCount) SelectCardUp();

        for (int i = 0; i < CardAssetCount; i++)
        {
            int num = GetCardNum(i);
            GameObject obj = transform.GetChild(i).gameObject;
            if (num >= gearManager.MaxCardCount)
                obj.SetActive(false);
            else
            {
                obj.SetActive(true);
                CardView card = obj.GetComponent<CardView>();
                for (int j = 0; j < card.gears.Count; j++)
                {
                    if(gearManager.GetCardData(num).gearList.Length > j && gearManager.GetCardData(num).gearList[j].data != null)
                    {
                        card.gears[j].gameObject.SetActive(true);
                        card.gears[j].sprite = gearManager.GetCardData(num).gearList[j].data.itemIcon9x9;
                    }
                    else
                        card.gears[j].gameObject.SetActive(false);
                }
            }
        }
    }

    public void SelectCard(int num)
    {
        Debug.Log("wa");
        if (num > CurrentCardNum) 
            for (int i = 0; i < num - CurrentCardNum + 1; i++) SelectCardUp();
        if (num < CurrentCardNum) 
            for (int i = 0; i < CurrentCardNum - num + 1; i++) SelectCardDown();
    }

    [NaughtyAttributes.Button]
    public void SelectCardUp()
    {
       if (CurrentCardNum <= 0) return;

       transform.GetChild(CardAssetCount - 1).SetSiblingIndex(CardAssetCount - CurrentCardNum - 1);
    }

    [NaughtyAttributes.Button]
    public void SelectCardDown()
    {
       if (CurrentCardNum >= gearManager.MaxCardCount - 1) return;
       
       transform.GetChild(CardAssetCount - CurrentCardNum - 2).SetAsLastSibling();
    }
}