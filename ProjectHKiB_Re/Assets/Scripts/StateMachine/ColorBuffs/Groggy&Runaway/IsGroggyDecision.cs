using UnityEngine;

namespace StateMachine
{
    [System.Serializable]
    public class IsGroggyDecision : StateDecision
    {
        public override bool Decide(StateController stateController)
        {
            if (stateController.TryGetInterface(out IAttackable attackable) &&
                attackable is AttackableModule attackableModule)
            {
#if UNITY_EDITOR
                //Debug.Log($"[Groggy DECISION] {stateController.name} | result={attackableModule.IsGroggy}");
#endif
                return attackableModule.IsGroggy;
            }

            Debug.LogError("ERROR: IAttackable not found.");
            return false;
        }
    }
}