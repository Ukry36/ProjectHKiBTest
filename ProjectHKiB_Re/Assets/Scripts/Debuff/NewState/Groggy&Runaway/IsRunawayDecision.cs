using UnityEngine;

[CreateAssetMenu(
    fileName = "IsRunawayDecision",
    menuName = "State Machine/Decision/Debuff/IsRunawayDecision"
)]
public class IsRunawayDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out IAttackable attackable) &&
            attackable is AttackableModule attackableModule)
        {
#if UNITY_EDITOR
            //Debug.Log($"[Runaway DECISION] {stateController.name} | result={attackableModule.IsRunaway}");
#endif
            return attackableModule.IsRunaway;
        }

        //Debug.LogError("ERROR: IAttackable not found.");
        return false;
    }
}