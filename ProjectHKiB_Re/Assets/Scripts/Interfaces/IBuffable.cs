using System.Collections.Generic;

public interface IBuffable : IInitializable
{
    List<BuffInfo> CurrentBuffs { get; set; }

    BuffInfo FindBuff(StatBuffSO buff);
    BuffInfo FindBuff(StatBuffSO buff, Gear sourceGear);

    BuffInfo Buff(StatBuffSO buff, int buffStack = 1, int timeStack = 1, float overrideTime = -1);
    BuffInfo Buff(StatBuffSO buff, Gear sourceGear, int buffStack = 1, int timeStack = 1, float overrideTime = -1);

    void UnBuff(StatBuffSO buff, int buffStack = 1, int reduceTime = 0, bool byTimer = false);
    void UnBuff(StatBuffSO buff, Gear sourceGear, int buffStack = 1, int reduceTime = 0, bool byTimer = false);
}