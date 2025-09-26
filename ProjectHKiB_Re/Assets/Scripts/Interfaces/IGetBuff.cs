using UnityEngine;

public interface IGetBuff : IInitializable
{
    void GetBuff(Transform target, StatBuffSO buff);
    public StatBuffSO Buff { get; set; }
}
