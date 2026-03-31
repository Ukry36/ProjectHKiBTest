using UnityEngine;

[CreateAssetMenu(fileName = "SuperArmourBuffType", menuName = "Scriptable Objects/StatBuffType/SuperArmourBuffType")]
public class SuperArmourBuffType : StatBuffTypeSO
{
    public override void AddBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, int multiplyer, bool stack)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.SuperArmourBuffer.AddBuff(buff, effectIndex, multiplyer, stack);
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, int multiplyer, bool remove)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.SuperArmourBuffer.RemoveBuff(buff, effectIndex, multiplyer, remove);
        }
    }
}