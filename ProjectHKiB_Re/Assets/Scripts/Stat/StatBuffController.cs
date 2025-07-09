using System;
using AYellowpaper.SerializedCollections;
using UnityEngine;
[Serializable]
public class BuffInfo
{
    public int ID { get => Buff.ID; }
    public StatBuffSO Buff { get; set; }
    public int Token { get; set; }
    public Cooltime Cooltime { get; set; }

    public BuffInfo(StatBuffSO buff, int stack)
    {
        Buff = buff;
        Token = stack;
        Cooltime = new();
    }
}
public class StatBuffController : InterfaceRegister
{
    [field: SerializeField] public SerializedDictionary<int, BuffInfo> CurrentBuffs { get; private set; } = new();

    /// <summary>
    /// Manages unbuffing.
    /// If buff is stackable, token decreases instead of unbuffing.
    /// But Unbuffs if token is turning less than 0 this time.
    /// It is okay to unbuff non existing buff.
    /// </summary>
    /// <param name="tokenReduction">buffInfo.Token -= tokenReduction</param>
    public void UnBuff(StatBuffSO buff, int tokenReduction = 1, float overrideTime = -1)
    {
        BuffInfo buffInfo = FindBuff(buff);
        if (buffInfo == null) return;
        if (buff.TimeStackable && buffInfo.Token > tokenReduction)
        {
            Buff(buff, -tokenReduction, overrideTime);
        }
        else
        {
            Debug.Log("unbuffed");
            buff.RemoveBuff(this);
            CurrentBuffs.Remove(buff.ID);
        }
    }

    public BuffInfo FindBuff(StatBuffSO buff)
    {
        if (CurrentBuffs.TryGetValue(buff.ID, out var buffInfo))
            return buffInfo;
        return null;
    }

    /// <summary>
    /// Manages buffing through controller.
    /// If you use this, the buff will be recorded in currentBuffs.
    /// This also manages cooltime and buff options automatically.
    /// To buff mannually, you can use ApplyBuff in StatBuffSO.
    /// </summary>
    /// <param name="token">
    /// Token is used by stackable and multiplyable option.
    /// </param>
    public BuffInfo Buff(StatBuffSO buff, int token = 1, float overrideTime = -1)
    {
        BuffInfo buffInfo = FindBuff(buff);

        // if original cooltime exists, start cooltime
        // if overrideTime exists, override original cooltime
        float cooltime = overrideTime > 0 ? overrideTime : buff.BuffTime > 0 ? buff.BuffTime : -1;
        if (buffInfo == null) // if buff isn't current, make new one and start cooltime
        {
            buffInfo = new(buff, 0);
            if (cooltime > 0)
                buffInfo.Cooltime.StartCooltime(cooltime, () => UnBuff(buff, 1, overrideTime));
        }
        else if (buff.UpdateBuffTime || buff.TimeStackable) // if buff is current and uses updateBuffTime, reset cooltime
        {
            if (cooltime > 0)
            {
                buffInfo.Cooltime.CancelCooltime();
                buffInfo.Cooltime.StartCooltime(cooltime, () => UnBuff(buff, 1, overrideTime));
            }
        }

        // if uses token and buff already exists, record token.
        if (buff.UseToken || buff.Multiplyable || buff.TimeStackable)
            buffInfo.Token += token;
        else
            buffInfo.Token = 1;

        // apply multiplied buff value
        if (buff.Multiplyable)
            buff.ApplyBuff(this, buffInfo.Token);
        else
            buff.ApplyBuff(this, 1);
        CurrentBuffs[buffInfo.ID] = buffInfo;
        return buffInfo;
    }
}