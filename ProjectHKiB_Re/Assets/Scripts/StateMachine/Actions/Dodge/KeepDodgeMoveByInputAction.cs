using UnityEngine;
[CreateAssetMenu(fileName = "KeepDodgeMoveByInput", menuName = "State Machine/Action/Dodge/KeepDodgeMoveByInput")]
public class KeepDodgeMoveByInputAction : StateActionSO
{
    public MovementManagerSO movementManager;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IMovable movable) && stateController.TryGetInterface(out IDodgeable dodgeable))
        {
            movementManager.WalkMove(stateController.transform, movable, dodgeable.BaseDodgeSpeed, GameManager.instance.inputManager.MoveInput, dodgeable.KeepDodgeWallLayer);
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
