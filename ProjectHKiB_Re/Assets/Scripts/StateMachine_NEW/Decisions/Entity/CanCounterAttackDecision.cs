using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class CanCounterAttackDecision : StateDecision
    {
        public override bool Decide(StateController stateController)
        {
            if (stateController.TryGetInterface(out IDodgeable dodgeable))
            {
                return dodgeable.CanCounterAttack;
            }
            Debug.LogError("ERROR: Interface Not Found!!!");
            return false;
        }
    }
}