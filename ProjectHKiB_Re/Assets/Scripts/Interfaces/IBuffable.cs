using System.Collections.Generic;

public interface IBuffable : IInitializable
{
    public List<BuffInfo> CurrentBuffs { get; set; }
    public BuffInfo FindBuff(StatBuffSO buff);
    public BuffInfo Buff(StatBuffSO buff, int buffStack = 1, int timeStack = 1, float overrideTime = -1);
    public void UnBuff(StatBuffSO buff, int buffStack = 1, int reduceTime = 0, bool byTimer = false);

}