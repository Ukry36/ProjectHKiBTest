using UnityEngine;
[CreateAssetMenu(fileName = "KeepDodgeMoveByInput", menuName = "Scriptable Objects/State Machine/Action/Dodge/KeepDodgeMoveByInput")]
public class KeepDodgeMoveByInputAction : StateActionSO
{
    public MovementManagerSO movementManager;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IMovable movable) && stateController.TryGetInterface(out IDodgeable dodgeable))
        {
            movementManager.WalkMove(stateController.transform, movable, GameManager.instance.inputManager.MoveInput, dodgeable.KeepDodgeWallLayer);
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
