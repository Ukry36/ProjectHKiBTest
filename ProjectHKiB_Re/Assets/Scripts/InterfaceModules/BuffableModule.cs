using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BuffInfo
{
    [field: SerializeField] public StatBuffSO Buff { get; set; }
    [field: SerializeField] public Gear SourceGear { get; set; }
    [field: SerializeField] public int BuffStack { get; set; }
    public Cooltime Cooltime { get; set; }

    public BuffInfo(StatBuffSO buff, Gear sourceGear)
    {
        Buff = buff;
        SourceGear = sourceGear;
        Cooltime = new();
    }

    public void AddBuff(InterfaceRegister interfaceReg, int multiplyer, bool stack)
    {
        if (stack) BuffStack += multiplyer;
        else BuffStack = multiplyer;

        Buff.AddBuff(interfaceReg, SourceGear, multiplyer, stack);
    }

    public void RemoveBuff(InterfaceRegister interfaceReg, int multiplyer, bool remove)
        => Buff.RemoveBuff(interfaceReg, SourceGear, multiplyer, remove);
}

public class BuffableModule : InterfaceModule, IBuffable
{
    public InterfaceRegister entityToBuff;
    [field: SerializeField] public List<BuffInfo> CurrentBuffs { get; set; } = new();

    [Header("Gear Source")]
    [SerializeField] private GearManager gearManager;
    [SerializeField] private int weaponSlotIndex = 0;

    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        interfaceRegistable.RegisterInterface<IBuffable>(this);
    }

    public void Initialize()
    {
        if (gearManager == null)
            gearManager = FindObjectOfType<GearManager>(true);
    }

    public BuffInfo FindBuff(StatBuffSO buff)
        => FindBuff(buff, GetCurrentSourceGear());

    public BuffInfo FindBuff(StatBuffSO buff, Gear sourceGear)
        => CurrentBuffs.Find(b => b.Buff == buff && b.SourceGear == sourceGear);

    public BuffInfo Buff(StatBuffSO buff, int buffStack = 1, int timeStack = 1, float overrideTime = -1)
        => Buff(buff, GetCurrentSourceGear(), buffStack, timeStack, overrideTime);

    public BuffInfo Buff(StatBuffSO buff, Gear sourceGear, int buffStack = 1, int timeStack = 1, float overrideTime = -1)
    {
        float cooltime = overrideTime > 0 ? overrideTime : buff.BuffTime;
        BuffInfo buffInfo = FindBuff(buff, sourceGear);

        if (buffInfo == null || buff.BuffStackType == StatBuffSO.BuffStackTypeEnum.Independant)
        {
            buffInfo = new(buff, sourceGear);
            buffInfo.AddBuff(entityToBuff, buffStack, false);

            if (!buffInfo.Buff.IsBuffTimeInfinite)
                buffInfo.Cooltime.StartCooltime(cooltime, () => UnBuff(buff, sourceGear));

            CurrentBuffs.Add(buffInfo);
        }
        else
        {
            if (buff.BuffStackType == StatBuffSO.BuffStackTypeEnum.Stack)
                buffInfo.AddBuff(entityToBuff, buffStack, true);
            else if (buff.BuffStackType == StatBuffSO.BuffStackTypeEnum.Overwrite)
                buffInfo.AddBuff(entityToBuff, buffStack, false);

            if (!buffInfo.Buff.IsBuffTimeInfinite)
            {
                if (buff.TimeStackType == StatBuffSO.TimeStackTypeEnum.Stack)
                {
                    float remain = buffInfo.Cooltime.RemainTime;
                    buffInfo.Cooltime.CancelCooltime();
                    buffInfo.Cooltime.StartCooltime(cooltime + remain, () => UnBuff(buff, sourceGear));
                }

                if (buff.TimeStackType == StatBuffSO.TimeStackTypeEnum.Overwrite)
                {
                    buffInfo.Cooltime.CancelCooltime();
                    buffInfo.Cooltime.StartCooltime(cooltime, () => UnBuff(buff, sourceGear));
                }
            }
        }

        return buffInfo;
    }

    public void UnBuff(StatBuffSO buff, int buffStack = 1, int reduceTime = 0, bool ignorePermanent = false)
        => UnBuff(buff, GetCurrentSourceGear(), buffStack, reduceTime, ignorePermanent);

    public void UnBuff(StatBuffSO buff, Gear sourceGear, int buffStack = 1, int reduceTime = 0, bool ignorePermanent = false)
    {
        BuffInfo buffInfo = FindBuff(buff, sourceGear);
        if (buffInfo == null) return;

        if (!ignorePermanent && buff.BuffRemoveType == StatBuffSO.BuffRemoveTypeEnum.Permanent)
            return;

        if (reduceTime > 0 && !buffInfo.Cooltime.IsCooltimeEnded)
        {
            float remain = buffInfo.Cooltime.RemainTime - reduceTime;
            buffInfo.Cooltime.CancelCooltime();
            if (remain > 0)
                buffInfo.Cooltime.StartCooltime(remain, () => UnBuff(buff, sourceGear));
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
            if (buffInfo.BuffStack <= 0)
                CurrentBuffs.Remove(buffInfo);
        }
    }

    public Gear GetCurrentSourceGear()
    {
        if (gearManager == null)
            gearManager = FindObjectOfType<GearManager>(true);

        if (gearManager == null) return null;
        return gearManager.GetCurrentWeaponGear(weaponSlotIndex);
    }
}