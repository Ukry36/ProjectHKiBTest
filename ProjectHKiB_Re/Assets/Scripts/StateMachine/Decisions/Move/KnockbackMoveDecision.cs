using UnityEngine;
[CreateAssetMenu(fileName = "KnockbackMoveDecision", menuName = "State Machine/Decision/Move/KnockbackMoveDecision")]
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
