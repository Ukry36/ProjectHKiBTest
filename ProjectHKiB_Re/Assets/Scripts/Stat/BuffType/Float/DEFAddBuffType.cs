using UnityEngine;

[CreateAssetMenu(fileName = "DEFAddBuffType", menuName = "Scriptable Objects/StatBuffType/DEFAddBuffType")]
public class DEFAddBuffType : StatBuffTypeSO
{
    public override void ApplyBuff(IInterfaceRegistable registable, StatBuffSO buff, int multiplyer)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.HealthController.DEFBuffer._statBuffAddList[buff.ID] = buff.Value * multiplyer;
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.HealthController.DEFBuffer.StatBuffAddList.Remove(buff.ID);
        }
    }
}