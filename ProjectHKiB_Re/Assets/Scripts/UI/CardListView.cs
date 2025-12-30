using UnityEngine;

public class CardListView: MonoBehaviour
{

    public GearManagerViewModel viewModel;

    private int CardCount => transform.childCount;

    private int CurrentCardNum => transform.GetChild(CardCount - 1).GetComponent<CardView>().cardNum;


    public void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        viewModel = new(GameManager.instance.gearManager);

        viewModel.RegistReactiveCommand(model =>
        {
            UpdateCards(model);
        }, this);
    }

    public void UpdateCards(GearManager gearManager)
    {
        for (int i = 0; i < CardCount; i++)
        {
            if (i < CardCount - gearManager.MaxCardCount)
                transform.GetChild(i).gameObject.SetActive(false);
            else
                transform.GetChild(i).gameObject.SetActive(true);
        }
    }

    [NaughtyAttributes.Button]
    public void Up()
    {
       int currentCardNumber =  CurrentCardNum;
       if (currentCardNumber >= CardCount - 1) return;
       
       transform.GetChild(CardCount - currentCardNumber - 2).SetAsLastSibling();
    }

    [NaughtyAttributes.Button]
    public void Down()
    {
        int currentCardNumber =  CurrentCardNum;
       if (currentCardNumber <= 0) return;

       transform.GetChild(CardCount - 1).SetSiblingIndex(CardCount - currentCardNumber - 1);
    }


}