using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class WalkByTargetDirAction : StateAction
    {
        [SerializeField] private bool _negate;
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetComponent(out IPhysics movable)
            && stateController.TryGetComponent(out ITargetable targetable))
            {
                if (targetable.CurrentTarget == null) return;
                Vector2 dir = targetable.CurrentTarget.position - stateController.transform.position;
                if (_negate) dir *= -1;
                movable.IsWalking = true;
                movable.WalkingDir = dir;
            }
            else
                Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}