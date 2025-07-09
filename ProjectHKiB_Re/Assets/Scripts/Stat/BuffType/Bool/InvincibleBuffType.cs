using UnityEngine;

[CreateAssetMenu(fileName = "InvincibleBuffType", menuName = "Scriptable Objects/StatBuffType/InvincibleBuffType")]
public class InvincibleBuffType : StatBuffTypeSO
{
    public override void ApplyBuff(IInterfaceRegistable registable, StatBuffSO buff, int multiplyer)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.HealthController.InvincibleBuffer.StatBuffList[buff.ID] = buff;
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.HealthController.InvincibleBuffer.StatBuffList.Remove(buff.ID);
        }
    }
}