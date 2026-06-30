using UnityEngine;

namespace StateMachine
{
    [System.Serializable]
    public class SelfDamageAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IAttackable attackable) &&
                attackable is AttackableModule attackableModule)
            {
                attackableModule.ExecuteSelfDamage();
            }
            else
            {
                Debug.LogError("ERROR: IAttackable not found.");
            }
        }
    }
}