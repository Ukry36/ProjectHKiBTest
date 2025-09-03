using UnityEngine;

public interface IGetBuff
{
    void GetBuff(Transform target, StatBuffSO buff);
    public StatBuffSO Buff { get; set; }
}
