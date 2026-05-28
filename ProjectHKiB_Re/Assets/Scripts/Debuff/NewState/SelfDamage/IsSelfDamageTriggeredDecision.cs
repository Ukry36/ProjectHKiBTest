using UnityEngine;

[CreateAssetMenu(
    fileName = "IsSelfDamageTriggeredDecision",
    menuName = "State Machine/Decision/Attack/IsSelfDamageTriggeredDecision"
)]
public class IsSelfDamageTriggeredDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out IAttackable attackable) &&
            attackable is AttackableModule attackableModule)
        {
        /*
    #if UNITY_EDITOR
            Debug.Log(
                $"[SelfDamage DECISION] {stateController.name} | " +
                $"result={attackableModule.IsSelfDamageTriggered}"
            );
    #endif
        */
            return attackableModule.IsSelfDamageTriggered;
        }

        Debug.LogError("ERROR: IAttackable not found.");
        return false;
    }
}