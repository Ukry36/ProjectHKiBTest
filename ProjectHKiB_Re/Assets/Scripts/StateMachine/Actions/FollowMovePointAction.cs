using UnityEngine;
[CreateAssetMenu(fileName = "FollowMovePointAction", menuName = "Scriptable Objects/State Machine/Action/FollowMovePoint", order = 3)]
public class FollowMovePointAction : StateActionSO
{
    public MovementManagerSO movementManager;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterFace(out IMovable movable))
        {
            movementManager.FollowMovePointIdle(stateController.transform, movable);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
