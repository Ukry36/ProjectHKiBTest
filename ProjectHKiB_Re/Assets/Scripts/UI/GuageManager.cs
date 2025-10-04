using Assets.Scripts.Interfaces.Modules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuageManager : MonoBehaviour
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
        viewmodel.RegistReactiveCommand(command =>
        {
            guage.fillAmount = command.HP / command.MaxHP;
            text.text = command.HP.ToString();
            Debug.Log("View changed: HP = " + command.HP);
        }, this);
    }
}