using UnityEngine;
[CreateAssetMenu(fileName = "CanAttackCooltimeDecision", menuName = "Scriptable Objects/State Machine/Decision/CanAttackCooltime", order = 4)]
public class CanAttackCooltimeDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out IAttackable attackable))
        {
            return !attackable.AttackController.isAttackCooltime;
        }
        return false;
    }
}
