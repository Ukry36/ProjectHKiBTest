using UnityEngine;

[CreateAssetMenu(fileName = "SuperArmourBuffType", menuName = "Scriptable Objects/StatBuffType/SuperArmourBuffType")]
public class SuperArmourBuffType : StatBuffTypeSO
{
    public override void AddBuff(IInterfaceRegistable registable, StatBuffSO buff, int multiplyer, bool stack)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.SuperArmourBuffer.AddBuff(buff, multiplyer, stack);
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff, int multiplyer, bool remove)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.SuperArmourBuffer.RemoveBuff(buff, multiplyer, remove);
        }
    }
}