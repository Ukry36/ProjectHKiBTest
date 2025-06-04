using System;
using System.Collections.Generic;

[Serializable]
public class StatBuffCompilation
{
    public StatBuffSO[] Buffs;

    public void EnableAllBuffs(StatBuffController buffControlller)
    {
        for (int i = 0; i < Buffs.Length; i++)
        {
            buffControlller.Buff(Buffs[i]);
        }
    }

    public void DisableAllBuffs(StatBuffController buffControlller)
    {
        for (int i = 0; i < Buffs.Length; i++)
        {
            buffControlller.UnBuff(Buffs[i]);
        }
    }
}