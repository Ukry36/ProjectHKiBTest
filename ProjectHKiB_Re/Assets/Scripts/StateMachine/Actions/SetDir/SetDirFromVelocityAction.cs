using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class SetDirFromVelocityAction : StateAction
    {
        [SerializeField] private bool negative;
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IPhysics movable))
            {
                Vector2 dir = movable.ExForce * (negative ? -1 : 1);
                if (stateController.TryGetInterface(out IDirAnimatable animatable))
                    animatable.SetAnimationDirection(dir);
            }

        }
    }
}