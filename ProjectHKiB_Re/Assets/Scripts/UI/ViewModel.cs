using System;
using R3;
using UnityEngine;

public class ViewModel<Model>
{
    protected Model _model;
    protected ReactiveCommand<Model> _command = new();

    public ViewModel(Model model)
    {
        _model = model;
    }

    public virtual void RegistReactiveCommand(Action<Model> subscribeAction, MonoBehaviour target)
    {
        _command.TakeUntil((a) => !target.enabled);
        _command.Subscribe(subscribeAction);
        _command.Execute(_model);
    }

    public void Dispose()
    {
        _command?.Dispose();
    }

    public void Execute()
    {
        _command.Execute(_model);
    }
}

public class DamagableViewModel : ViewModel<IDamagable>
{
    public DamagableViewModel(IDamagable model) : base(model) { }

    public override void RegistReactiveCommand(Action<IDamagable> subscribeAction, MonoBehaviour target)
    {
        base.RegistReactiveCommand(subscribeAction, target);
        _model.OnHPChanged += (a) => _command.Execute(_model);
        _model.OnDamaged += () => _command.Execute(_model);
        _model.OnHealed += () => _command.Execute(_model);
        _model.MaxHPBuffer.OnBuffed += (a) => _command.Execute(_model);
    }
}

public class GearManagerViewModel : ViewModel<GearManager>
{
    public GearManagerViewModel(GearManager model) : base(model) { }

    public override void RegistReactiveCommand(Action<GearManager> subscribeAction, MonoBehaviour target)
    {
        base.RegistReactiveCommand(subscribeAction, target);
        _model.OnMaxCardChanged += () => _command.Execute(_model);
        _model.OnMaxSlotChanged += () => _command.Execute(_model);
        _model.OnSetCardData += () => _command.Execute(_model);
    }

    public void SetGearData(int cardIndex, int gearSlotIndex, Gear gear)
    {
        _model.SetGearData(cardIndex, gearSlotIndex, gear);
    }
}

public class GearInventoryViewModel : ViewModel<InventoryManager>
{
    public GearInventoryViewModel(InventoryManager model) : base(model) { }

    public override void RegistReactiveCommand(Action<InventoryManager> subscribeAction, MonoBehaviour target)
    {
        base.RegistReactiveCommand(subscribeAction, target);
        _model.OnGearInventoryChanged += () => _command.Execute(_model);
    }
}