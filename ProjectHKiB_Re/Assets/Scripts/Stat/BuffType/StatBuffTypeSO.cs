using UnityEngine;

public abstract class StatBuffTypeSO : ScriptableObject
{
    public abstract void AddBuff(IInterfaceRegistable registable, StatBuffSO buff, int multiplyer, bool stack);
    public abstract void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff, int multiplyer, bool remove);
}