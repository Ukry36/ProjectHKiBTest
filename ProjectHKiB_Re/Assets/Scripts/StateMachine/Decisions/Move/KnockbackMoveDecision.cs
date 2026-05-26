using UnityEngine;
[CreateAssetMenu(fileName = "KnockbackMoveDecision", menuName = "State Machine/Decision/Move/KnockbackMoveDecision")]
public class KnockbackMoveDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        Debug.LogError("NOT IMPLEMENTED!!!!!!!!!!!!!!");
        if (stateController.TryGetInterface(out IPhysics movable))
            return false;
        Debug.LogError("ERROR: Interface Not Found!!!");
        return false;
    }
}
