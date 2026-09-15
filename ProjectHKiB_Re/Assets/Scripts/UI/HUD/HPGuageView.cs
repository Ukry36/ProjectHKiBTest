using Assets.Scripts.Interfaces.Modules;
using TMPro;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class HPGuageView : MonoBehaviour
{
    public Image guage;
    public TextMeshProUGUI hpText;
    [SerializeField] private DamagableModule target;

    public DamagableViewModel viewmodel;

    public void Start()
    {
        RegisterTarget(target);
    }

    public void RegisterTarget(IDamagable target)
    {
        viewmodel = new(target);
        viewmodel.HpRatio.Subscribe(ratio => guage.fillAmount = ratio).AddTo(this);
        viewmodel.HpText.Subscribe (text  => {if (hpText) hpText.text = text;}).AddTo(this);
    }
}