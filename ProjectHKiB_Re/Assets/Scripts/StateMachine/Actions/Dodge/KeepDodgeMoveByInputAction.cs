using UnityEngine;
[CreateAssetMenu(fileName = "KeepDodgeMoveByInput", menuName = "State Machine/Action/Dodge/KeepDodgeMoveByInput")]
public class KeepDodgeMoveByInputAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IMovable movable) && stateController.TryGetInterface(out IDodgeable dodgeable))
        {
            Debug.LogError("ERROR: Not Implemented!!!");

            //movementManager.WalkMove(stateController.transform, movable, dodgeable.BaseDodgeSpeed, GameManager.instance.inputManager.MoveInput, dodgeable.KeepDodgeWallLayer);
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
