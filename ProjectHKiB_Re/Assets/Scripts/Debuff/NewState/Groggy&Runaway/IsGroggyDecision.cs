using UnityEngine;

[CreateAssetMenu(
    fileName = "IsGroggyDecision",
    menuName = "State Machine/Decision/Debuff/IsGroggyDecision"
)]
public class IsGroggyDecision : StateDecisionSO
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