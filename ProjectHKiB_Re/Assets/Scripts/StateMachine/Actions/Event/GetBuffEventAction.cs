using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "GetBuffEvent", menuName = "State Machine/Action/Event/GetBuffEvent")]
public class GetBuffEventAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IGetBuff getBuff) && stateController.TryGetInterface(out IEvent @event))
        {
            getBuff.GetBuff(@event.CurrentTarget, getBuff.Buff);
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
