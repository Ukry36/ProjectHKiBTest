using UnityEngine;

[CreateAssetMenu(fileName = "ATKAddBuffType", menuName = "Scriptable Objects/StatBuffType/ATKAddBuffType")]
public class ATKAddBuffType : StatBuffTypeSO
{
    public override void ApplyBuff(IInterfaceRegistable registable, StatBuffSO buff, int multiplyer)
    {
        if (registable.TryGetInterface(out IAttackable attackable))
        {
            attackable.AttackController.ATKBuffer._statBuffAddList[buff.ID] = buff.Value * multiplyer;
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff)
    {
        if (registable.TryGetInterface(out IAttackable attackable))
        {
            attackable.AttackController.ATKBuffer.StatBuffAddList.Remove(buff.ID);
        }
    }
}