using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class KeepDodgeEndedDecision : StateDecision
    {
        public override bool Decide(StateController stateController)
        {
            if (stateController.TryGetInterface(out IDodgeable dodgeable))
            {
                return dodgeable.CheckKeepDodgeEnded();
            }
            Debug.LogError("ERROR: Interface Not Found!!!");
            return false;
        }
    }
}