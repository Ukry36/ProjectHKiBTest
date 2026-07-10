using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class TargetEntityManipulateAction : StateAction
    {
        public string targetID;
        [SerializeReference, SubclassSelector] public StateAction targetAction;
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IEvent @event) && @event.CurrentTargets.targetEntities.ContainsKey(targetID))
            {
                targetAction.Act(@event.CurrentTargets.targetEntities[targetID].Target);
            }
            else Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}