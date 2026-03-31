using UnityEngine;

[CreateAssetMenu(fileName = "SpeedBuffType", menuName = "Scriptable Objects/StatBuffType/SpeedBuffType")]
public class SpeedBuffType : StatBuffTypeSO
{
    public override void AddBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, int multiplyer, bool stack)
    {
        if (registable.TryGetInterface(out IMovable movable))
        {
            movable.SpeedBuffer.AddBuff(buff, effectIndex, multiplyer, stack);
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, int multiplyer, bool remove)
    {
        if (registable.TryGetInterface(out IMovable movable))
        {
            movable.SpeedBuffer.RemoveBuff(buff, effectIndex, multiplyer, remove);
        }
    }
}