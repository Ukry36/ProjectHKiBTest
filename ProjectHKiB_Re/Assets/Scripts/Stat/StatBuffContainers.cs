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

    public float BuffedStat => baseStat + StatBuffAdd + StatBuffProp * baseStat;

    public float StatBuffAdd
    {
        get
        {
            float result = 0;
            foreach (var item in _statBuffAddList) result += item.Value;
            return result;
        }
    }

    public float StatBuffProp
    {
        get
        {
            float result = 0;
            foreach (var item in _statBuffPropList) result += item.Value;
            return result;
        }
    }

    public void AddBuff(StatBuffSO buff, int effectIndex, int multiplyer, bool stack)
    {
        var effect = buff.GetEffect(effectIndex);
        if (effect == null) return;

        int effectID = buff.GetEffectID(effectIndex);

        if (effect.IsDebuff) multiplyer *= -1;

        if (effect.IsValuePropositional)
        {
            if (_statBuffPropList.ContainsKey(effectID) && stack)
                _statBuffPropList[effectID] += effect.Value * multiplyer;
            else
                _statBuffPropList[effectID] = effect.Value * multiplyer;
        }
        else
        {
            if (_statBuffAddList.ContainsKey(effectID) && stack)
                _statBuffAddList[effectID] += effect.Value * multiplyer;
            else
                _statBuffAddList[effectID] = effect.Value * multiplyer;
        }

        OnBuffed?.Invoke(BuffedStat);
    }

    public void RemoveBuff(StatBuffSO buff, int effectIndex, int multiplyer, bool remove)
    {
        var effect = buff.GetEffect(effectIndex);
        if (effect == null) return;

        int effectID = buff.GetEffectID(effectIndex);

        if (_statBuffAddList.ContainsKey(effectID))
        {
            if (effect.IsDebuff)
            {
                _statBuffAddList[effectID] += effect.Value * multiplyer;
                if (_statBuffAddList[effectID] > effect.Value * 0.5f || remove)
                    _statBuffAddList.Remove(effectID);
            }
            else
            {
                _statBuffAddList[effectID] -= effect.Value * multiplyer;
                if (_statBuffAddList[effectID] < effect.Value * 0.5f || remove)
                    _statBuffAddList.Remove(effectID);
            }
        }

        if (_statBuffPropList.ContainsKey(effectID))
        {
            if (effect.IsDebuff)
            {
                _statBuffPropList[effectID] += effect.Value * multiplyer;
                if (_statBuffPropList[effectID] > effect.Value * 0.5f || remove)
                    _statBuffPropList.Remove(effectID);
            }
            else
            {
                _statBuffPropList[effectID] -= effect.Value * multiplyer;
                if (_statBuffPropList[effectID] < effect.Value * 0.5f || remove)
                    _statBuffPropList.Remove(effectID);
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
            foreach (var item in _statBuffList) val += item.Value;
            return val > 0;
        }
    }

    public bool BuffedStat => StatBuff;

    public void AddBuff(StatBuffSO buff, int effectIndex, int multiplyer, bool stack)
    {
        var effect = buff.GetEffect(effectIndex);
        if (effect == null) return;

        int effectID = buff.GetEffectID(effectIndex);

        if (_statBuffList.ContainsKey(effectID) && stack)
            _statBuffList[effectID] += effect.IsDebuff ? -multiplyer : multiplyer;
        else
            _statBuffList[effectID] = effect.IsDebuff ? -multiplyer : multiplyer;

        OnBuffed?.Invoke(BuffedStat);
    }

    public void RemoveBuff(StatBuffSO buff, int effectIndex, int multiplyer, bool remove)
    {
        var effect = buff.GetEffect(effectIndex);
        if (effect == null) return;

        int effectID = buff.GetEffectID(effectIndex);

        if (_statBuffList.ContainsKey(effectID))
        {
            if (effect.IsDebuff)
            {
                _statBuffList[effectID] += multiplyer;
                if (_statBuffList[effectID] > effect.Value * 0.5f || remove)
                    _statBuffList.Remove(effectID);
            }
            else
            {
                _statBuffList[effectID] -= multiplyer;
                if (_statBuffList[effectID] < effect.Value * 0.5f || remove)
                    _statBuffList.Remove(effectID);
            }
        }

        OnBuffed?.Invoke(BuffedStat);
    }
}