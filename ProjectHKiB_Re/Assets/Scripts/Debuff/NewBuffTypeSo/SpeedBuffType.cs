using UnityEngine;

[CreateAssetMenu(fileName = "SpeedBuffType", menuName = "Scriptable Objects/StatBuffType/SpeedBuffType")]
public class SpeedBuffType : StatBuffTypeSO
{
    public override void AddBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, Gear sourceGear, int multiplyer, bool stack)
    {
        if (registable.TryGetInterface(out IMovable movable))
        {
            movable.SpeedBuffer.AddBuff(buff, effectIndex, sourceGear, multiplyer, stack);
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, Gear sourceGear, int multiplyer, bool remove)
    {
        if (registable.TryGetInterface(out IMovable movable))
        {
            movable.SpeedBuffer.RemoveBuff(buff, effectIndex, sourceGear, multiplyer, remove);
        }
    }
}