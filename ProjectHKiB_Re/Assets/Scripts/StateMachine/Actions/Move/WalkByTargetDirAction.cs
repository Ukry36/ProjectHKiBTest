using UnityEngine;
[CreateAssetMenu(fileName = "WalkByTargetDir", menuName = "Scriptable Objects/State Machine/Action/Move/WalkByTargetDir")]
public class WalkByTargetDirAction : StateActionSO
{
    [SerializeField] private bool _negate;
    [SerializeField] private MovementManagerSO movementManager;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetComponent(out IMovable movable)
        && stateController.TryGetComponent(out ITargetable targetable))
        {
            Vector2 dir = targetable.CurrentTarget.position - stateController.transform.position;
            if (_negate) dir *= -1;
            movementManager.WalkMove(stateController.transform, movable, movable.Speed, dir, movable.WallLayer);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
