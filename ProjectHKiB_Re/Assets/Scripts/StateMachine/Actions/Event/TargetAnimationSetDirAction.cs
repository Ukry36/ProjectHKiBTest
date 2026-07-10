using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class TargetAnimationSetDirAction : StateAction
    {
        public string targetID;
        public EnumManager.AnimDir dir;
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IEvent @event) && @event.CurrentTargets.targetAnimations.ContainsKey(targetID))
            {
                @event.CurrentTargets.targetAnimations[targetID].Target.SetDirection(dir);
            }
            else Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}