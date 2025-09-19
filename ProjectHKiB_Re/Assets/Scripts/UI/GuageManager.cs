using Loxodon.Framework.Binding;
using Loxodon.Framework.Binding.Builder;
using Loxodon.Framework.Binding.Contexts;

using Loxodon.Framework.Contexts;
using Loxodon.Framework.Observables;

using Loxodon.Framework.ViewModels;
using Loxodon.Framework.Views;

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Guage : ObservableObject
{
    private float _max;
    private float _value;

    public float Max { get => _max; set => Set(ref _max, value); }
    public float Value { get => _value; set => Set(ref _value, value); }
}

public class GuageViewModel : ViewModelBase
{
    private float _max;
    private float _value;

    public float Max { get => _max; set => Set(ref _max, value); }
    public float Value { get => _value; set => Set(ref this._value, value); }

    public void OnMaxChanged(int max) => _max = max;
    public void OnValueChanged(int value) => _value = value;
}

public class GuageManager : UIView
{
    public Image guageOutline;
    public Image guage;
    public TextMeshProUGUI text;
    public GameObject damagable;

    protected override void Awake()
    {
        ApplicationContext context = Context.GetApplicationContext();
        BindingServiceBundle bindingService = new(context.GetContainer());
        bindingService.Start();
    }

    protected override void Start()
    {
        IDamagable damagable = this.damagable.GetComponent<IDamagable>();
        Guage guage = new() { Max = 1000, Value = 0 };
        GuageViewModel guageViewModel = new();

        IBindingContext bindingContext = this.BindingContext();
        bindingContext.DataContext = guageViewModel;

        BindingSet<GuageManager, GuageViewModel> bindingSet = this.CreateBindingSet<GuageManager, GuageViewModel>();
        bindingSet.Bind(this.guage).For(v => v.fillAmount).To(vm => vm.Value / vm.Max).OneWay();
        bindingSet.Bind(damagable).For(v => v.OnHPChanged).To<int>(vm => vm.OnValueChanged);
        bindingSet.Bind(damagable).For(v => v.OnMaxHPChanged).To<int>(vm => vm.OnMaxChanged);
        bindingSet.Build();
    }
}