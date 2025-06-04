using UnityEngine;

[CreateAssetMenu(fileName = "MaxHPAddBuffType", menuName = "Scriptable Objects/StatBuffType/MaxHPAddBuffType")]
public class MaxHPAddBuffType : StatBuffTypeSO
{
    public override void ApplyBuff(IInterfaceRegistable registable, StatBuffSO buff, int multiplyer)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.HealthController.MaxHPBuffer.StatBuffAddList[buff.ID] = buff.Value * multiplyer;
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.HealthController.MaxHPBuffer.StatBuffAddList.Remove(buff.ID);
        }
    }
}