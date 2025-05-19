using UnityEngine;
[CreateAssetMenu(fileName = "ChainEvent", menuName = "Scriptable Objects/State Machine/Action/ChainEvent")]
public class ChainEventAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IChainEventable chain) && stateController.TryGetInterface(out IEvent @event))
        {
            if (@event.CurrentTarget)
                chain.ChainEvent.RegisterTarget(@event.CurrentTarget);
            chain.ChainEvent.TriggerEvent();
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
