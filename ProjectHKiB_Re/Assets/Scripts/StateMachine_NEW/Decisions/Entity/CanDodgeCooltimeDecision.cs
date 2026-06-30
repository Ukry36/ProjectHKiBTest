using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class CanDodgeCooltimeDecision : StateDecision
    {
        public override bool Decide(StateController stateController)
        {
            if (stateController.TryGetInterface(out IDodgeable dodgeable))
            {
                return dodgeable.CanDodge;
            }
            Debug.LogError("ERROR: Interface Not Found!!!");
            return false;
        }
    }
}