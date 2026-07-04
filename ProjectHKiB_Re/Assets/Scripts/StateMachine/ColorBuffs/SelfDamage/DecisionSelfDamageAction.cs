using UnityEngine;

namespace StateMachine
{
    [System.Serializable]
    public class DecisionSelfDamageAction : StateAction
    {
        [SerializeField] private int damageNumber;

        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IAttackable attackable) &&
                attackable is AttackableModule attackableModule)
            {
                attackableModule.RollSelfDamage(damageNumber);
                /*
                #if UNITY_EDITOR
                        Debug.Log(
                            $"[SelfDamage ROLL] {stateController.name} | " +
                            $"chance={attackableModule.SelfDamageChance:F2} | " +
                            $"result={attackableModule.IsSelfDamageTriggered}"
                        );
                #endif
                */
            }
            else
            {
                Debug.LogError("ERROR: IAttackable not found.");
            }
        }
    }
}