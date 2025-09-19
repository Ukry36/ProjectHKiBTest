using UnityEngine;

[CreateAssetMenu(fileName = "ATKPropBuffType", menuName = "Scriptable Objects/StatBuffType/ATKPropBuffType")]
public class ATKPropBuffType : StatBuffTypeSO
{
    public override void ApplyBuff(IInterfaceRegistable registable, StatBuffSO buff, int multiplyer)
    {
        if (registable.TryGetInterface(out IAttackable attackable))
        {
            attackable.AttackController.ATKBuffer._statBuffPropList[buff.ID] = buff.Value * multiplyer;
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff)
    {
        if (registable.TryGetInterface(out IAttackable attackable))
        {
            attackable.AttackController.ATKBuffer.StatBuffPropList.Remove(buff.ID);
        }
    }
}