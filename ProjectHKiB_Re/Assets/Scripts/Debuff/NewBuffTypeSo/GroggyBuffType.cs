using UnityEngine;

[CreateAssetMenu(
    fileName = "GroggyBuffType",
    menuName = "Scriptable Objects/StatBuffType/GroggyBuffType"
)]
public class GroggyBuffType : StatBuffTypeSO
{
    public override void AddBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, Gear sourceGear, int multiplyer, bool stack)
    {
        if (registable.TryGetInterface(out IAttackable attackable) &&
            attackable is AttackableModule attackableModule)
        {
            attackableModule.GroggyBuffer.AddBuff(buff, effectIndex, sourceGear, multiplyer, stack);
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, Gear sourceGear, int multiplyer, bool remove)
    {
        if (registable.TryGetInterface(out IAttackable attackable) &&
            attackable is AttackableModule attackableModule)
        {
            attackableModule.GroggyBuffer.RemoveBuff(buff, effectIndex, sourceGear, multiplyer, remove);
        }
    }
}