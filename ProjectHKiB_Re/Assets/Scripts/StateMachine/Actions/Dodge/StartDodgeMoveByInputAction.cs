using UnityEngine;
[CreateAssetMenu(fileName = "StartDodgeMoveByInput", menuName = "State Machine/Action/Dodge/StartDodgeMoveByInput")]
public class StartDodgeMoveByInputAction : StateActionSO
{
    public MovementManagerSO movementManager;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IMovable movable) && stateController.TryGetInterface(out IDodgeable dodgeable))
        {
            Vector3 dir = GameManager.instance.inputManager.MoveInput;
            if (dir != Vector3.zero)
                movementManager.InitialDodgeMove(stateController.transform, movable, dodgeable, dir);
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
