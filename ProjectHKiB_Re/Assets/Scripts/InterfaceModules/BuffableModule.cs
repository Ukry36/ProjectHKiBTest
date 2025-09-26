
using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class BuffInfo
{
    public StatBuffSO Buff { get; set; }
    public int BuffStack { get; set; }
    public Cooltime Cooltime { get; set; }

    public BuffInfo(StatBuffSO buff, int buffStack)
    {
        Buff = buff;
        BuffStack = buffStack;
        Cooltime = new();
    }

    public void AddBuff(InterfaceRegister interfaceReg, int multiplyer, bool stack)
    => Buff.AddBuff(interfaceReg, multiplyer, stack);

    public void RemoveBuff(InterfaceRegister interfaceReg, int multiplyer, bool remove)
    => Buff.RemoveBuff(interfaceReg, multiplyer, remove);
}
public class BuffableModule : InterfaceModule, IBuffable
{
    public InterfaceRegister entityToBuff;
    [field: SerializeField] public List<BuffInfo> CurrentBuffs { get; set; } = new();

    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        interfaceRegistable.RegisterInterface<IBuffable>(this);
    }

    public void Initialize()
    {

    }

    public BuffInfo FindBuff(StatBuffSO buff) => CurrentBuffs.Find(b => b.Buff == buff);

    /// <summary>
    /// </summary>
    /// <param name="buffStack">
    /// </param>
    public BuffInfo Buff(StatBuffSO buff, int buffStack = 1, int timeStack = 1, float overrideTime = -1)
    {
        float cooltime = overrideTime > 0 ? overrideTime : buff.BuffTime;

        BuffInfo buffInfo = FindBuff(buff);
        if (buffInfo == null || buff.BuffStackType == StatBuffSO.BuffStackTypeEnum.Independant)
        {
            buffInfo = new(buff, buffStack);
            buffInfo.AddBuff(entityToBuff, buffStack, true);


            buffInfo.Cooltime.StartCooltime(cooltime, () => UnBuff(buff, 1)); // #
            CurrentBuffs.Add(buffInfo);
        }
        else
        {
            if (buff.BuffStackType == StatBuffSO.BuffStackTypeEnum.Stack)
                buffInfo.AddBuff(entityToBuff, buffStack, true);
            if (buff.BuffStackType == StatBuffSO.BuffStackTypeEnum.Overwrite)
                buffInfo.AddBuff(entityToBuff, buffStack, false);


            if (buff.TimeStackType == StatBuffSO.TimeStackTypeEnum.Stack)
            {
                float remain = buffInfo.Cooltime.RemainTime;
                buffInfo.Cooltime.CancelCooltime();
                buffInfo.Cooltime.StartCooltime(cooltime + remain, () => UnBuff(buff, 1)); //#
            }
            if (buff.TimeStackType == StatBuffSO.TimeStackTypeEnum.Overwrite)
            {
                buffInfo.Cooltime.CancelCooltime();
                buffInfo.Cooltime.StartCooltime(cooltime, () => UnBuff(buff, 1)); //#
            }
        }

        return buffInfo;
    }

    /// <summary>
    /// </summary>
    /// <param name="buffStack">
    /// </param>
    public void UnBuff(StatBuffSO buff, int buffStack = 1, int reduceTime = 0)
    {
        BuffInfo buffInfo = FindBuff(buff);
        if (buffInfo == null) return;

        if (reduceTime > 0 && !buffInfo.Cooltime.IsCooltimeEnded)
        {
            float remain = buffInfo.Cooltime.RemainTime - reduceTime;
            buffInfo.Cooltime.CancelCooltime();
            if (remain > 0) buffInfo.Cooltime.StartCooltime(remain, () => UnBuff(buff, 1));
        }

        if (buff.BuffRemoveType == StatBuffSO.BuffRemoveTypeEnum.Remove
    || buff.BuffStackType == StatBuffSO.BuffStackTypeEnum.Independant)
        {
            buffInfo.RemoveBuff(entityToBuff, 1, true);
            CurrentBuffs.Remove(buffInfo);
        }
        if (buff.BuffRemoveType == StatBuffSO.BuffRemoveTypeEnum.Unstack)
        {
            buffInfo.RemoveBuff(entityToBuff, buffStack, false);
            buffInfo.BuffStack -= buffStack;
            if (buffInfo.BuffStack <= 0) CurrentBuffs.Remove(buffInfo);
        }
    }
}