using Assets.Scripts.Interfaces.Modules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HPGuageView : MonoBehaviour
{
    public Image guageOutline;
    public Image guage;
    public TextMeshProUGUI text;
    [SerializeField] private DamagableModule target;

    public DamagableViewModel viewmodel;

    public void Start()
    {
        RegisterTarget(target);
    }

    public void RegisterTarget(IDamagable target)
    {
        viewmodel = new(target);
        viewmodel.RegistReactiveCommand(model =>
        {
            guage.fillAmount = model.HP / model.MaxHP;
            text.text = model.HP.ToString();
            Debug.Log("View changed: HP = " + model.HP);
        }, this);
    }
}