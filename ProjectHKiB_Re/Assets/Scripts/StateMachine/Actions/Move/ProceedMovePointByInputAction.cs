using UnityEngine;
[CreateAssetMenu(fileName = "ProceedMovePointByInput", menuName = "State Machine/Action/Move/ProceedMovePointByInput")]
public class ProceedMovePointByInputAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IMovable movable))
        {
            //movementManager.ProceedMovePoint(stateController.transform, movable, GameManager.instance.inputManager.MoveInput);
            Debug.LogError("ERROR: Not Implemented!!!");
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
