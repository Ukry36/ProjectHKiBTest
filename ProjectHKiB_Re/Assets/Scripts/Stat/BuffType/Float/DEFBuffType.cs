using UnityEngine;

[CreateAssetMenu(fileName = "DEFBuffType", menuName = "Scriptable Objects/StatBuffType/DEFBuffType")]
public class DEFBuffType : StatBuffTypeSO
{
    public override void AddBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, int multiplyer, bool stack)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.DEFBuffer.AddBuff(buff, effectIndex, multiplyer, stack);
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, int multiplyer, bool remove)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.DEFBuffer.RemoveBuff(buff, effectIndex, multiplyer, remove);
        }
    }
}