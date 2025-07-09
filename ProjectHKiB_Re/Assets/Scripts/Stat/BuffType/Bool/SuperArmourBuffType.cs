using UnityEngine;

[CreateAssetMenu(fileName = "SuperArmourBuffType", menuName = "Scriptable Objects/StatBuffType/SuperArmourBuffType")]
public class SuperArmourBuffType : StatBuffTypeSO
{
    public override void ApplyBuff(IInterfaceRegistable registable, StatBuffSO buff, int multiplyer)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.HealthController.SuperArmourBuffer.StatBuffList[buff.ID] = buff;
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.HealthController.SuperArmourBuffer.StatBuffList.Remove(buff.ID);
        }
    }
}