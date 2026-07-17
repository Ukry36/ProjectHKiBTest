using UnityEngine;

[CreateAssetMenu(
    fileName = "RunawayBuffType",
    menuName = "Scriptable Objects/StatBuffType/RunawayBuffType"
)]
public class RunawayBuffType : StatBuffTypeSO
{
    public override void AddBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, Gear sourceGear, int multiplyer, bool stack)
    {
        if (registable.TryGetInterface(out IAttackable attackable) &&
            attackable is AttackableModule attackableModule)
        {
            attackableModule.RunawayBuffer.AddBuff(buff, effectIndex, sourceGear, multiplyer, stack);
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, Gear sourceGear, int multiplyer, bool remove)
    {
        if (registable.TryGetInterface(out IAttackable attackable) &&
            attackable is AttackableModule attackableModule)
        {
            attackableModule.RunawayBuffer.RemoveBuff(buff, effectIndex, sourceGear, multiplyer, remove);
        }
    }
}