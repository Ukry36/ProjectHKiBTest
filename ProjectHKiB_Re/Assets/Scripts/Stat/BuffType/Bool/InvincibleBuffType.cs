using UnityEngine;

[CreateAssetMenu(fileName = "InvincibleBuffType", menuName = "Scriptable Objects/StatBuffType/InvincibleBuffType")]
public class InvincibleBuffType : StatBuffTypeSO
{
    public override void AddBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, int multiplyer, bool stack)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.InvincibleBuffer.AddBuff(buff, effectIndex, multiplyer, stack);
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, int multiplyer, bool remove)
    {
        if (registable.TryGetInterface(out IDamagable damagable))
        {
            damagable.InvincibleBuffer.RemoveBuff(buff, effectIndex, multiplyer, remove);
        }
    }
}