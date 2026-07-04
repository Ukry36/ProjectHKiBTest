using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class DamageAction : StateAction
    {
        public int damageNumber;
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IAttackable attackable))
            {
                attackable.Attack(damageNumber);
            }
            else
                Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}