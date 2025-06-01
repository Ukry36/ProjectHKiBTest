using UnityEngine;
[CreateAssetMenu(fileName = "WalkByPathFind", menuName = "Scriptable Objects/State Machine/Action/Move/WalkByPathFindAction")]
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
                movementManager.WalkMove(stateController.transform, movable, movable.Speed, targetPos - stateController.transform.position, movable.WallLayer);
            }
            else
            {
                movementManager.WalkMove(stateController.transform, movable, movable.Speed, attackable.CurrentTarget.position - stateController.transform.position, movable.WallLayer);
            }
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}
