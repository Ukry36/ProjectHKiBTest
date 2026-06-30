using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class SetAttackCooltimeAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IAttackable attackable))
            {
                attackable.StartAttackCooltime();
            }
            else
                Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}