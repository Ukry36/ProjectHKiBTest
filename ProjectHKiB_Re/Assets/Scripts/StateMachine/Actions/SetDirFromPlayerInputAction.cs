using UnityEngine;
[CreateAssetMenu(fileName = "SetDirFromPlayerInputAction", menuName = "Scriptable Objects/State Machine/Action/SetDirFromPlayerInput", order = 3)]
public class SetDirFromPlayerInputAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        stateController.animationController.SetAnimationDirection(GameManager.instance.inputManager.MoveInput);
    }
}
