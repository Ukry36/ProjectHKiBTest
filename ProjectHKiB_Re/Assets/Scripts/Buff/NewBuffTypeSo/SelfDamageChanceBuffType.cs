using UnityEngine;

[CreateAssetMenu(
    fileName = "SelfDamageChanceBuffType",
    menuName = "Scriptable Objects/StatBuffType/SelfDamageChanceBuffType"
)]
public class SelfDamageChanceBuffType : StatBuffTypeSO
{
    public override void AddBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, Gear sourceGear, int multiplyer, bool stack)
    {
        if (registable.TryGetInterface(out IAttackable attackable) &&
            attackable is AttackableModule attackableModule)
        {
            attackableModule.SelfDamageChanceBuffer.AddBuff(buff, effectIndex, sourceGear, multiplyer, stack);
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, Gear sourceGear, int multiplyer, bool remove)
    {
        if (registable.TryGetInterface(out IAttackable attackable) &&
            attackable is AttackableModule attackableModule)
        {
            attackableModule.SelfDamageChanceBuffer.RemoveBuff(buff, effectIndex, sourceGear, multiplyer, remove);
        }
    }
}