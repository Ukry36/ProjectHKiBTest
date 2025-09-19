using System;
using System.Collections.Generic;

public class FloatBuffContainer
{
    public FloatBuffContainer(float baseStat)
    {
        this.baseStat = baseStat;
    }
    public readonly float baseStat;
    public void AddAddBuff(StatBuffSO buff, int multiplyer)
    {
        _statBuffAddList[buff.ID] = buff.Value * multiplyer;
        OnBuffed?.Invoke(this, BuffedStat);
    }
    public void AddPropBuff(StatBuffSO buff, int multiplyer)
    {
        _statBuffPropList[buff.ID] = buff.Value * multiplyer;
        OnBuffed?.Invoke(this, BuffedStat);
    }
    private readonly Dictionary<int, float> _statBuffAddList = new(10);
    public float StatBuffAdd
    {
        get
        {
            float result = 0;
            foreach (var item in _statBuffAddList)
            {
                result += item.Value;
            }
            return result;
        }
    }
    private readonly Dictionary<int, float> _statBuffPropList = new(10);
    public float StatBuffProp
    {
        get
        {
            float result = 0;
            foreach (var item in _statBuffPropList)
            {
                result += item.Value;
            }
            return result;
        }
    }
    public float GetBuffedStat()
    => baseStat * (1 + StatBuffProp) + StatBuffAdd;

    public float BuffedStat { get => baseStat + StatBuffAdd + StatBuffProp * baseStat; }

    public EventHandler<float> OnBuffed;
}

public class BoolBuffCalculator
{
    public Dictionary<int, bool> StatBuffList { get; set; } = new(10);
    public bool StatBuff
    {
        get
        {
            foreach (var item in StatBuffList)
            {
                if (item.Value)
                {
                    return true;
                }
            }
            return false;
        }
    }
    public bool GetBuffedStat()
    => StatBuff;
}