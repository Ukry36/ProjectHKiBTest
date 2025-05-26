using UnityEngine;
[CreateAssetMenu(fileName = "ProceedMovePointByInput", menuName = "Scriptable Objects/State Machine/Action/Move/ProceedMovePointByInput")]
public class ProceedMovePointByInputAction : StateActionSO
{
    public MovementManagerSO movementManager;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IMovable movable))
        {
            movementManager.ProceedMovePoint(stateController.transform, movable, GameManager.instance.inputManager.MoveInput);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
