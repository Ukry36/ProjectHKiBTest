using UnityEngine;

[CreateAssetMenu(fileName = "AttackCooltimeBuffType", menuName = "Scriptable Objects/StatBuffType/AttackCooltimeBuffType")]
public class AttackCooltimeBuffType : StatBuffTypeSO
{
    // IsDebuff=true + Value=0.2 → 공격 쿨타임 20% 증가
    public override void AddBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, Gear sourceGear, int multiplyer, bool stack)
    {
        if (registable.TryGetInterface(out IAttackable attackable) && attackable is AttackableModule attackableModule)
        {
            attackableModule.AttackCooltimeBuffer.AddBuff(buff, effectIndex, sourceGear, multiplyer, stack);
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, Gear sourceGear, int multiplyer, bool remove)
    {
        if (registable.TryGetInterface(out IAttackable attackable) && attackable is AttackableModule attackableModule)
        {
            attackableModule.AttackCooltimeBuffer.RemoveBuff(buff, effectIndex, sourceGear, multiplyer, remove);
        }
    }
}