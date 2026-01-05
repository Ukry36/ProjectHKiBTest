using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;

public class CardListView: MonoBehaviour
{

    public GearManagerViewModel viewModel;

    public GearManager gearManager;

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
            if (i >= gearManager.MaxCardCount)
                obj.SetActive(false);
            else
            {
                obj.SetActive(true);
                CardView card = obj.GetComponent<CardView>();
                for (int j = 0; j < card.gears.Count; j++)
                {
                    if(gearManager.GetCardData(i).gearList.Length > j && gearManager.GetCardData(i).gearList[j].data != null)
                    {
                        card.gears[j].gameObject.SetActive(true);
                        card.gears[j].sprite = gearManager.GetCardData(i).gearList[j].data.itemIcon9x9;
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
    }
}