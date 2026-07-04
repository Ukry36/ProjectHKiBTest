using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class CanAttackCooltimeDecision : StateDecision
    {
        public override bool Decide(StateController stateController)
        {
            if (stateController.TryGetInterface(out IAttackable attackable))
            {
                return !attackable.IsAttackCooltime;
            }
            return false;
        }
    }
}