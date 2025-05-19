using UnityEngine;
[CreateAssetMenu(fileName = "SetDirFromPlayerInputAction", menuName = "Scriptable Objects/State Machine/Action/SetDirFromPlayerInput", order = 3)]
public class SetDirFromPlayerInputAction : StateActionSO
{
    [SerializeField] private bool negative;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IEntityStateController controller))
            if (!controller.AnimationController.CheckIfLastSetDirectionSame(GameManager.instance.inputManager.MoveInput))
                controller.AnimationController.SetAnimationDirection(negative ? GameManager.instance.inputManager.MoveInput * -1 : GameManager.instance.inputManager.MoveInput);
    }
}