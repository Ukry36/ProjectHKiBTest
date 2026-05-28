using System;
using System.Collections.Generic;
using UnityEngine;

public class FloatBuffContainer
{
    public FloatBuffContainer(float baseStat, float minStat = float.NegativeInfinity)
    {
        this.baseStat = baseStat;
        _minStat = minStat;
    }

    public readonly float baseStat;
    // 버프 적용 후 최솟값 제한. Speed=0f(역방향 방지), MaxHP=1f(0 이하 방지), ATK=0f / 기본값=제한 없음
    private readonly float _minStat;
    public Action<float> OnBuffed;

    private readonly Dictionary<int, float> _statBuffAddList = new(10);
    private readonly Dictionary<int, float> _statBuffPropList = new(10);

    public float BuffedStat => Mathf.Max(_minStat, baseStat + StatBuffAdd + StatBuffProp * baseStat);

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
        => AddBuff(buff, effectIndex, null, multiplyer, stack);

    public void AddBuff(StatBuffSO buff, int effectIndex, Gear sourceGear, int multiplyer, bool stack)
    {
        var effect = buff.GetEffect(effectIndex);
        if (effect == null) return;

        int effectID = buff.GetEffectID(effectIndex, sourceGear);

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
        => RemoveBuff(buff, effectIndex, null, multiplyer, remove);

    public void RemoveBuff(StatBuffSO buff, int effectIndex, Gear sourceGear, int multiplyer, bool remove)
    {
        var effect = buff.GetEffect(effectIndex);
        if (effect == null) return;

        int effectID = buff.GetEffectID(effectIndex, sourceGear);

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
        => AddBuff(buff, effectIndex, null, multiplyer, stack);

    public void AddBuff(StatBuffSO buff, int effectIndex, Gear sourceGear, int multiplyer, bool stack)
    {
        var effect = buff.GetEffect(effectIndex);
        if (effect == null) return;

        int effectID = buff.GetEffectID(effectIndex, sourceGear);

        if (_statBuffList.ContainsKey(effectID) && stack)
            _statBuffList[effectID] += effect.IsDebuff ? -multiplyer : multiplyer;
        else
            _statBuffList[effectID] = effect.IsDebuff ? -multiplyer : multiplyer;

        OnBuffed?.Invoke(BuffedStat);
    }

    public void RemoveBuff(StatBuffSO buff, int effectIndex, int multiplyer, bool remove)
        => RemoveBuff(buff, effectIndex, null, multiplyer, remove);

    public void RemoveBuff(StatBuffSO buff, int effectIndex, Gear sourceGear, int multiplyer, bool remove)
    {
        var effect = buff.GetEffect(effectIndex);
        if (effect == null) return;

        int effectID = buff.GetEffectID(effectIndex, sourceGear);

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

public class CooltimeMultiplierBuffContainer
{
    public Action<float> OnBuffed;

    private readonly float _baseMultiplier;
    private readonly Dictionary<int, float> _cooltimeBuffList = new(10);

    public CooltimeMultiplierBuffContainer(float baseMultiplier = 1f)
    {
        _baseMultiplier = baseMultiplier;
    }

    public float BuffedMultiplier
    {
        get
        {
            float result = _baseMultiplier;
            foreach (var item in _cooltimeBuffList)
                result += item.Value;

            return Mathf.Max(0f, result);
        }
    }

    public void AddBuff(StatBuffSO buff, int effectIndex, int multiplyer, bool stack)
        => AddBuff(buff, effectIndex, null, multiplyer, stack);

    public void AddBuff(StatBuffSO buff, int effectIndex, Gear sourceGear, int multiplyer, bool stack)
    {
        var effect = buff.GetEffect(effectIndex);
        if (effect == null) return;

        int effectID = buff.GetEffectID(effectIndex, sourceGear);

        float value = effect.Value * multiplyer;

        // Debuff면 쿨타임 증가, Buff면 쿨타임 감소
        if (!effect.IsDebuff)
            value *= -1f;

        if (_cooltimeBuffList.ContainsKey(effectID) && stack)
            _cooltimeBuffList[effectID] += value;
        else
            _cooltimeBuffList[effectID] = value;

        OnBuffed?.Invoke(BuffedMultiplier);
    }

    public void RemoveBuff(StatBuffSO buff, int effectIndex, int multiplyer, bool remove)
        => RemoveBuff(buff, effectIndex, null, multiplyer, remove);

    public void RemoveBuff(StatBuffSO buff, int effectIndex, Gear sourceGear, int multiplyer, bool remove)
    {
        var effect = buff.GetEffect(effectIndex);
        if (effect == null) return;

        int effectID = buff.GetEffectID(effectIndex, sourceGear);
        if (!_cooltimeBuffList.ContainsKey(effectID)) return;

        float value = effect.Value * multiplyer;

        // AddBuff와 같은 부호 규칙을 맞춰야 정상 제거됨
        if (!effect.IsDebuff)
            value *= -1f;

        _cooltimeBuffList[effectID] -= value;

        if (remove || Mathf.Abs(_cooltimeBuffList[effectID]) <= 0.0001f)
            _cooltimeBuffList.Remove(effectID);

        OnBuffed?.Invoke(BuffedMultiplier);
    }
}