using UnityEngine;
[CreateAssetMenu(fileName = "SetDirFromLastSetDirAction", menuName = "State Machine/Action/SetDir/SetDirFromLastSetDir")]
public class SetDirFromLastSetDirAction : StateActionSO
{
    [SerializeField] private bool negative;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IMovable movable))
        {
            if (stateController.TryGetInterface(out IDirAnimatable animatable))
            {
                animatable.SetAnimationDirection(movable.LastSetDir);
            }

        }

    }
}
