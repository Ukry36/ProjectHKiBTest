using UnityEngine;

[CreateAssetMenu(fileName = "MaxHPPropBuffType", menuName = "Scriptable Objects/StatBuffType/MaxHPPropBuffType")]
public class MaxHPPropBuffType : StatBuffTypeSO
{
    public override void ApplyBuff(IInterfaceRegistable registable, StatBuffSO buff, int multiplyer)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.HealthController.MaxHPBuffer.StatBuffPropList[buff.ID] = buff.Value * multiplyer;
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.HealthController.MaxHPBuffer.StatBuffPropList.Remove(buff.ID);
        }
    }
}