using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class EndEventAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IEvent @event))
            {
                foreach (Collider2D col in @event.CurrentTargets) @event.EndEvent(col);
            }
            else
                Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}