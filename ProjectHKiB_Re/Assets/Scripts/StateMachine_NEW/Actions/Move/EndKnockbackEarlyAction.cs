using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class EndKnockbackEarlyAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IPhysics movable))
            {
                movable.EndKnockbackEarly();
            }
            else Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}