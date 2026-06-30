using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class KnockbackMoveDecision : StateDecision
    {
        public override bool Decide(StateController stateController)
        {
            Debug.LogError("NOT IMPLEMENTED!!!!!!!!!!!!!!");
            if (stateController.TryGetInterface(out IPhysics movable))
                return false;
            Debug.LogError("ERROR: Interface Not Found!!!");
            return false;
        }
    }
}