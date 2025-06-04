using UnityEngine;

[CreateAssetMenu(fileName = "DEFPropBuffType", menuName = "Scriptable Objects/StatBuffType/DEFPropBuffType")]
public class DEFPropBuffType : StatBuffTypeSO
{
    public override void ApplyBuff(IInterfaceRegistable registable, StatBuffSO buff, int multiplyer)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.HealthController.DEFBuffer.StatBuffPropList[buff.ID] = buff.Value * multiplyer;
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.HealthController.DEFBuffer.StatBuffPropList.Remove(buff.ID);
        }
    }
}