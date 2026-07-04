using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class ChainEventAction : StateAction
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
}