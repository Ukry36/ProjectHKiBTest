using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class SupplyEventAction : StateAction
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
}