using UnityEngine;
[CreateAssetMenu(fileName = "SupplyEvent", menuName = "State Machine/Action/Event/SupplyEvent")]
public class SupplyEventAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out ISupply supply) && stateController.TryGetInterface(out IEvent @event))
        {
            foreach (Collider2D col in @event.CurrentTargets)
            {
                Transform transform = col.transform;
                supply.Supply(transform, supply.Amount);
            }
            
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }

}
