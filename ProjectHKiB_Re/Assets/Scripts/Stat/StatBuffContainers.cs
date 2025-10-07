using System;
using System.Collections.Generic;
using UnityEngine;

public class FloatBuffContainer
{
    public FloatBuffContainer(float baseStat)
    {
        this.baseStat = baseStat;
    }
    public readonly float baseStat;
    public Action<float> OnBuffed;
    private readonly Dictionary<int, float> _statBuffAddList = new(10);
    private readonly Dictionary<int, float> _statBuffPropList = new(10);
    public float BuffedStat { get => baseStat + StatBuffAdd + StatBuffProp * baseStat; }
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

    public void AddBuff(StatBuffSO buff, int multiplyer, bool stack)
    {
        if (buff.IsDebuff) multiplyer *= -1;
        if (buff.IsValuePropositional)
        {
            if (_statBuffPropList.ContainsKey(buff.ID) && stack)
                _statBuffPropList[buff.ID] += buff.Value * multiplyer;
            else
                _statBuffPropList[buff.ID] = buff.Value * multiplyer;
        }
        else
        {
            if (_statBuffAddList.ContainsKey(buff.ID) && stack)
                _statBuffAddList[buff.ID] += buff.Value * multiplyer;
            else
                _statBuffAddList[buff.ID] = buff.Value * multiplyer;
        }
        OnBuffed?.Invoke(BuffedStat);
    }
    public void RemoveBuff(StatBuffSO buff, int multiplyer, bool remove)
    {
        if (_statBuffAddList.ContainsKey(buff.ID))
        {
            if (buff.IsDebuff)
            {
                _statBuffAddList[buff.ID] += buff.Value * multiplyer;
                if (_statBuffAddList[buff.ID] > buff.Value * 0.5f || remove)
                    _statBuffAddList.Remove(buff.ID);
            }
            else
            {
                _statBuffAddList[buff.ID] -= buff.Value * multiplyer;
                if (_statBuffAddList[buff.ID] < buff.Value * 0.5f || remove)
                    _statBuffAddList.Remove(buff.ID);
            }
        }
        if (_statBuffPropList.ContainsKey(buff.ID))
        {
            if (buff.IsDebuff)
            {
                _statBuffPropList[buff.ID] += buff.Value * multiplyer;
                if (_statBuffPropList[buff.ID] > buff.Value * 0.5f || remove)
                    _statBuffPropList.Remove(buff.ID);
            }
            else
            {
                _statBuffPropList[buff.ID] -= buff.Value * multiplyer;
                if (_statBuffPropList[buff.ID] < buff.Value * 0.5f || remove)
                    _statBuffPropList.Remove(buff.ID);
            }
        }

        OnBuffed?.Invoke(BuffedStat);
    }
}

public class BoolBuffContainer
{
    public Action<bool> OnBuffed;
    private readonly Dictionary<int, int> _statBuffList = new(10);
    public bool StatBuff
    {
        get
        {
            int val = 0;
            foreach (var item in _statBuffList)
            {
                val += item.Value;
            }
            return val > 0;
        }
    }
    public bool BuffedStat { get => StatBuff; }

    public void AddBuff(StatBuffSO buff, int multiplyer, bool stack)
    {
        if (_statBuffList.ContainsKey(buff.ID) && stack)
            _statBuffList[buff.ID] += buff.IsDebuff ? -multiplyer : multiplyer;
        else
            _statBuffList[buff.ID] = buff.IsDebuff ? -multiplyer : multiplyer;
        OnBuffed?.Invoke(BuffedStat);
    }
    public void RemoveBuff(StatBuffSO buff, int multiplyer, bool remove)
    {
        if (_statBuffList.ContainsKey(buff.ID))
        {
            if (buff.IsDebuff)
            {
                _statBuffList[buff.ID] += multiplyer;
                if (_statBuffList[buff.ID] > buff.Value * 0.5f || remove)
                    _statBuffList.Remove(buff.ID);
            }
            else
            {
                _statBuffList[buff.ID] -= multiplyer;
                if (_statBuffList[buff.ID] < buff.Value * 0.5f || remove)
                    _statBuffList.Remove(buff.ID);
            }
        }

        OnBuffed?.Invoke(BuffedStat);
    }
}