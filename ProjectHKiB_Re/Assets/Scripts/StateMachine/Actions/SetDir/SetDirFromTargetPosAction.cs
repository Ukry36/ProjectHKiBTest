using UnityEngine;
[CreateAssetMenu(fileName = "SetDirFromTargetPos", menuName = "State Machine/Action/SetDir/SetDirFromTargetPos")]
public class SetDirFromTargetPosAction : StateActionSO
{
    [SerializeField] private bool negative;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDirAnimatable animatable)
        && stateController.TryGetInterface(out ITargetable targetable))
        {
            if (!targetable.CurrentTarget) return;
            Vector2 dir = targetable.CurrentTarget.position - stateController.transform.position;
            if (!animatable.AnimationController.CheckIfLastSetDirectionSame(dir))
                animatable.AnimationController.SetAnimationDirection(negative ? dir * -1 : dir);
        }

    }
}
