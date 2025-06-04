using UnityEngine;

public abstract class StatBuffTypeSO : ScriptableObject
{
    public abstract void ApplyBuff(IInterfaceRegistable registable, StatBuffSO buff, int multiplyer);
    public abstract void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff);
}