using UnityEngine;

[CreateAssetMenu(fileName = "ResistanceBuffType", menuName = "Scriptable Objects/StatBuffType/ResistanceBuffType")]
public class ResistanceBuffType : StatBuffTypeSO
{
    public override void AddBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, int multiplyer, bool stack)
    {
        if (registable is InterfaceRegister interfaceRegister &&
            interfaceRegister.TryGetInterface(out IDamagable damagable))
        {
            damagable.ResistanceBuffer.AddBuff(buff, effectIndex, multiplyer, stack);
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, int multiplyer, bool stack)
    {
        if (registable is InterfaceRegister interfaceRegister &&
            interfaceRegister.TryGetInterface(out IDamagable damagable))
        {
            damagable.ResistanceBuffer.RemoveBuff(buff, effectIndex, multiplyer, stack);
        }
    }
}