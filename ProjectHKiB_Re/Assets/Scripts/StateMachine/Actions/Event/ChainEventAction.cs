using UnityEngine;
[CreateAssetMenu(fileName = "ChainEvent", menuName = "State Machine/Action/Event/ChainEvent")]
public class ChainEventAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IChainEventable chain) && stateController.TryGetInterface(out IEvent @event))
        {
            if (@event.TargetCount > 0)
                chain.ChainEvent.RegisterTarget(@event.CurrentTargets, @event.TargetCount);
            chain.ChainEvent.TriggerEvent();
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
