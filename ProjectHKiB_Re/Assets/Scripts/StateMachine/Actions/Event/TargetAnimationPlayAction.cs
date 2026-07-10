using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class TargetAnimationPlayAction : StateAction
    {
        public string targetID;
        public string animationName;
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IEvent @event) && @event.CurrentTargets.targetAnimations.ContainsKey(targetID))
            {
                @event.CurrentTargets.targetAnimations[targetID].Target.Play(animationName);
            }
            else Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}