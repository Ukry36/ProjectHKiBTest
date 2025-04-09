using UnityEngine;
[CreateAssetMenu(fileName = "SetDirFromVelocityAction", menuName = "Scriptable Objects/State Machine/Action/SetDirFromVelocity", order = 3)]
public class SetDirFromVelocityAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        stateController.animationController.SetAnimationDirection(stateController.velocity);
    }
}
