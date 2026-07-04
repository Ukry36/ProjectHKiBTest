using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class SetDirFromTargetPosAction : StateAction
    {
        [SerializeField] private bool negative;
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IDirAnimatable animatable)
            && stateController.TryGetInterface(out ITargetable targetable))
            {
                if (!targetable.CurrentTarget) return;
                Vector2 dir = targetable.CurrentTarget.position - stateController.transform.position;
                if (!animatable.CheckIfLastSetDirectionSame(dir))
                    animatable.SetAnimationDirection(negative ? dir * -1 : dir);
            }

        }
    }
}