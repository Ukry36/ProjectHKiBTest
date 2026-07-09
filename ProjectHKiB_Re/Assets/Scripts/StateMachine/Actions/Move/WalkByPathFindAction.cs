using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class WalkByPathFindAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IPhysics movable)
            && stateController.TryGetInterface(out IPathFindable pathfindable)
            && stateController.TryGetInterface(out ITargetable targetable))
            {
                if (pathfindable.IsPathValid)
                {
                    Vector3 targetPos = pathfindable.NextPath;
                    //Debug.DrawLine(targetPos - Vector3.one * 0.5f, targetPos + Vector3.one * 0.5f);
                    movable.IsWalking = true;
                    movable.WalkingDir = targetPos - stateController.transform.position;
                }
                else
                {
                    if (targetable.CurrentTarget == null) return;
                    movable.IsWalking = true;
                    movable.WalkingDir = targetable.CurrentTarget.position - stateController.transform.position;
                }
            }
            else
                Debug.LogError("ERROR: Interface Not Found!!!");

        }
    }
}
