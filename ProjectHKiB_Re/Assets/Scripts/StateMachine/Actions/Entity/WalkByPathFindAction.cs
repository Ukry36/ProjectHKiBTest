using UnityEngine;
[CreateAssetMenu(fileName = "WalkByPathFind", menuName = "Scriptable Objects/State Machine/Action/WalkByPathFindAction", order = 3)]
public class WalkByPathFindAction : StateActionSO
{
    [SerializeField] private MovementManagerSO movementManager;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetComponent(out IMovable movable) && stateController.TryGetComponent(out IAttackable attackable) && stateController.TryGetComponent(out IPathFindable pathfindable))
        {
            if (pathfindable.PathList != null && pathfindable.PathList.Count > 0)
            {
                Vector3 targetPos = pathfindable.PathList[0];
                //Debug.DrawLine(targetPos - Vector3.one * 0.5f, targetPos + Vector3.one * 0.5f);
                movementManager.WalkMove(stateController.transform, movable, targetPos - stateController.transform.position);
            }
            else
            {
                movementManager.WalkMove(stateController.transform, movable, attackable.CurrentTarget.position - stateController.transform.position);
            }
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}
