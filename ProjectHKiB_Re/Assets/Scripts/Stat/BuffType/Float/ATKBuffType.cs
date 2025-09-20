using UnityEngine;

[CreateAssetMenu(fileName = "ATKBuffType", menuName = "Scriptable Objects/StatBuffType/ATKBuffType")]
public class ATKBuffType : StatBuffTypeSO
{
    public override void AddBuff(IInterfaceRegistable registable, StatBuffSO buff, int multiplyer, bool stack)
    {
        if (registable.TryGetInterface(out IAttackable attackable))
        {
            attackable.ATKBuffer.AddBuff(buff, multiplyer, stack);
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff, int multiplyer, bool remove)
    {
        if (registable.TryGetInterface(out IAttackable attackable))
        {
            attackable.ATKBuffer.RemoveBuff(buff, multiplyer, remove);
        }
    }
}