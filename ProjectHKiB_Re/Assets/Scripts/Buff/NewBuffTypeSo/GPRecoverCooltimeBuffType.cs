using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GPRecoverCooltimeBuffType", menuName = "Scriptable Objects/StatBuffType/GPRecoverCooltimeBuffType")]
public class GPRecoverCooltimeBuffType : StatBuffTypeSO
{
    public override void AddBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, Gear sourceGear, int multiplyer, bool stack)
    {
        if (registable is Component component && component.TryGetComponent(out IGPRecoverBuffable gpRecoverBuffable))
        {
            gpRecoverBuffable.GPRecoverCooltimeBuffer.AddBuff(buff, effectIndex, sourceGear, multiplyer, stack);
            gpRecoverBuffable.RefreshGPRecoverTimer();
        }
    }

    public override void RemoveBuff(IInterfaceRegistable registable, StatBuffSO buff, int effectIndex, Gear sourceGear, int multiplyer, bool remove)
    {
        if (registable is Component component && component.TryGetComponent(out IGPRecoverBuffable gpRecoverBuffable))
        {
            gpRecoverBuffable.GPRecoverCooltimeBuffer.RemoveBuff(buff, effectIndex, sourceGear, multiplyer, remove);
            gpRecoverBuffable.RefreshGPRecoverTimer();
        }
    }
}
