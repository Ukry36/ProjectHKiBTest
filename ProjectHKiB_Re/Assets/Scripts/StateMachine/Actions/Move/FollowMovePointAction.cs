using UnityEngine;
[CreateAssetMenu(fileName = "FollowMovePointAction", menuName = "State Machine/Action/Move/FollowMovePoint")]
public class FollowMovePointAction : StateActionSO
{
    public MovementManagerSO movementManager;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IMovable movable))
        {
            movementManager.FollowMovePointIdle(stateController.transform, movable);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
