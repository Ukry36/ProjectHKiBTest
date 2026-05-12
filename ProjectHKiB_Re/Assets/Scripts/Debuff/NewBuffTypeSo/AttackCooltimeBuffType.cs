using UnityEngine;

[CreateAssetMenu(fileName = "AttackCooltimeBuffType", menuName = "Scriptable Objects/StatBuffType/AttackCooltimeBuffType")]
public class AttackCooltimeBuffType : StatBuffTypeSO
{
    // Value만큼 배수로 쿨타임이 더해짐. (ex Value=0.2&IsDebuff=true -> 쿨타임 20% 증가), 플레이어는 쿨타임이 0초입니다.
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