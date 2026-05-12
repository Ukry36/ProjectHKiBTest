using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "AccuracyMissChanceBuffType",
    menuName = "Scriptable Objects/StatBuffType/AccuracyMissChanceBuffType"
)]
public class AccuracyMissChanceBuffType : StatBuffTypeSO
{
    public override void AddBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, Gear sourceGear, int multiplyer, bool stack)
    {
        if (registable.TryGetInterface(out IAttackable attackable) &&
            attackable is AttackableModule attackableModule)
        {
            attackableModule.AccuracyMissChanceBuffer.AddBuff(buff, effectIndex, sourceGear, multiplyer, stack);
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, Gear sourceGear, int multiplyer, bool remove)
    {
        if (registable.TryGetInterface(out IAttackable attackable) &&
            attackable is AttackableModule attackableModule)
        {
            attackableModule.AccuracyMissChanceBuffer.RemoveBuff(buff, effectIndex, sourceGear, multiplyer, remove);
        }
    }
}