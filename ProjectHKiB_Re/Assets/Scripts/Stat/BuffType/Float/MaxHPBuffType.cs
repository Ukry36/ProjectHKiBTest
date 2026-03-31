using UnityEngine;

[CreateAssetMenu(fileName = "MaxHPBuffType", menuName = "Scriptable Objects/StatBuffType/MaxHPBuffType")]
public class MaxHPBuffType : StatBuffTypeSO
{
    public override void AddBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, int multiplyer, bool stack)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.MaxHPBuffer.AddBuff(buff, effectIndex, multiplyer, stack);
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, int multiplyer, bool remove)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.MaxHPBuffer.RemoveBuff(buff, effectIndex, multiplyer, remove);
        }
    }
}