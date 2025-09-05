using UnityEngine;
[CreateAssetMenu(fileName = "CanCounterAttack", menuName = "State Machine/Decision/CanCounterAttack")]
public class CanCounterAttackDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDodgeable dodgeable))
        {
            return dodgeable.CanCounterAttack;
        }
        Debug.LogError("ERROR: Interface Not Found!!!");
        return false;
    }
}
