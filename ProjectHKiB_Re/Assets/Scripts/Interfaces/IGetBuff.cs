using UnityEngine;

public interface IGetBuff
{
    void GetBuff(Transform target, StatBuffController buffController, StatBuffSO buff);
    public StatBuffSO Buff { get; set; }
    public int Multiplyer { get; set; }
}
