using System;
using System.Collections.Generic;

[Serializable]
public class StatBuffCompilation
{
    public StatBuffSO[] Buffs;

    public void EnableAllBuffs(BuffableModule buffControlller)
    {
        for (int i = 0; i < Buffs.Length; i++)
        {
            buffControlller.Buff(Buffs[i]);
        }
    }

    public void DisableAllBuffs(BuffableModule buffControlller)
    {
        for (int i = 0; i < Buffs.Length; i++)
        {
            buffControlller.UnBuff(Buffs[i]);
        }
    }
}