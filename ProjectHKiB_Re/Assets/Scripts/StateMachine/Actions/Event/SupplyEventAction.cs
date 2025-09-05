using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "SupplyEvent", menuName = "State Machine/Action/Event/SupplyEvent")]
public class SupplyEventAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out ISupply supply) && stateController.TryGetInterface(out IEvent @event))
        {
            supply.Supply(@event.CurrentTarget, supply.Amount);
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }

}
