using UnityEngine;
[CreateAssetMenu(fileName = "KnockbackMoveDecision", menuName = "Scriptable Objects/State Machine/Decision/KnockbackMoveDecision", order = 4)]
public class KnockbackMoveDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out IMovable movable))
            return movable.IsKnockbackMove;
        Debug.LogError("ERROR: Interface Not Found!!!");
        return false;
    }
}
