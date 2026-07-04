using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class SetDirFromLastSetDirAction : StateAction
    {
        [SerializeField] private bool negative;
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IPhysics movable))
            {
                if (stateController.TryGetInterface(out IDirAnimatable animatable))
                {
                    animatable.SetAnimationDirection(movable.LastSetDir);
                }

            }

        }
    }
}