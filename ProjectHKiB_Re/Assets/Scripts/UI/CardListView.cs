using UnityEngine;

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
            if (num >= gearManager.MaxCardCount)
                transform.GetChild(i).gameObject.SetActive(false);
            else
                transform.GetChild(i).gameObject.SetActive(true);
        }
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