using System;
using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine;

public abstract class ViewModel<Model> : IDisposable
{
    protected Model _model;

    public abstract void Dispose();

    protected abstract void UpdateViewModelState();

}

public class DamagableViewModel : ViewModel<IDamagable>
{
    public ReadOnlyReactiveProperty<float> HpRatio => _hpRatio;
    public ReadOnlyReactiveProperty<string> HpText => _hpText;

    private readonly ReactiveProperty<float> _hpRatio;
    private readonly ReactiveProperty<string> _hpText;

    public DamagableViewModel(IDamagable model)
    {
        _model = model;

        _hpRatio = new ReactiveProperty<float>((float)_model.HP / _model.MaxHP);
        _hpText = new ReactiveProperty<string>($"{_model.HP}/{_model.MaxHP}");

        _model.OnHPChanged += (a) => UpdateViewModelState();
        _model.OnDamaged += () => UpdateViewModelState();
        _model.OnHealed += () => UpdateViewModelState();
        _model.MaxHPBuffer.OnBuffed += (a) => UpdateViewModelState();
    }

    protected override void UpdateViewModelState()
    {
        _hpRatio.Value = (float)_model.HP / _model.MaxHP;
        _hpText.Value = $"{_model.HP}/{_model.MaxHP}";
    }

    public override void Dispose()
    {
        _hpRatio?.Dispose();
        _hpText?.Dispose();
    }
}

public class GearManagerViewModel : ViewModel<GearManager>
{
    public ReadOnlyReactiveProperty<List<CardData>> CardList => _cardList;
    public ReadOnlyReactiveProperty<int> CurrentCard => _currentCard;
    public ReadOnlyReactiveProperty<int> CurrentSlot => _currentSlot;

    private readonly ReactiveProperty<List<CardData>> _cardList;
    private readonly ReactiveProperty<int> _currentCard;
    private readonly ReactiveProperty<int> _currentSlot;

    public GearManagerViewModel(GearManager model)
    {
        _model = model;

        _cardList = new ReactiveProperty<List<CardData>>(_model.playerCardEquipData);
        _currentCard = new ReactiveProperty<int>(_model.CurrentEdittingCard);
        _currentSlot = new ReactiveProperty<int>(_model.CurrentEdittingSlot);
        
        _model.OnMaxCardChanged += () => UpdateViewModelState();
        _model.OnMaxSlotChanged += () => UpdateViewModelState();
        _model.OnSetCardData += () => UpdateViewModelState();
    }
    protected override void UpdateViewModelState()
    {
        _cardList.Value = _model.playerCardEquipData;
        _cardList.ForceNotify();
    }
    
    public void SetGearData(Gear gear)
    {
        _model.SetGearData(_model.CurrentEdittingCard, _model.CurrentEdittingSlot, gear);
    }

    public CardData GetCardData(int index)
    {
        return _cardList.Value.GetSafe(index);
    }

    public void SetCurrentEdittingCard(int index)
    {
        _model.CurrentEdittingCard = index;
        _currentCard.Value = index;
        _currentCard.ForceNotify();
    }

    public void SetCurrentEdittingSlot(int index)
    {
        _model.CurrentEdittingSlot = index;
        _currentSlot.Value = index;
        _currentSlot.ForceNotify();
    }

    public override void Dispose()
    {
        _cardList?.Dispose();
    }
}

public class InventoryViewModel : ViewModel<InventoryManager>
{
    public ReadOnlyReactiveProperty<List<Gear>> GearList => _gearList;
    public ReadOnlyReactiveProperty<List<Item>> ItemList => _itemList;

    private readonly ReactiveProperty<List<Gear>> _gearList;
    private readonly ReactiveProperty<List<Item>> _itemList;

    public InventoryViewModel(InventoryManager model)
    {
        _model = model;

        _gearList = new ReactiveProperty<List<Gear>>(_model.playerGearInventory.Values.ToList());
        _itemList = new ReactiveProperty<List<Item>>(_model.playerInventory.Values.ToList());

        _model.OnInventoryChanged += () => UpdateViewModelState();
        _model.OnGearInventoryChanged += () => UpdateViewModelState();
    }

    protected override void UpdateViewModelState()
    {
        _gearList.Value = _model.playerGearInventory.Values.ToList();
        _gearList.ForceNotify();
        _itemList.Value = _model.playerInventory.Values.ToList();
        _itemList.ForceNotify();
    }

    public Gear GetGear(int index) => _gearList.Value.GetSafe(index);
    

    public override void Dispose()
    {
        _gearList?.Dispose();
        _itemList?.Dispose();
    }
}