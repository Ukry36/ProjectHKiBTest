using UnityEngine;
[CreateAssetMenu(fileName = "CanDodgeCooltime", menuName = "State Machine/Decision/CanDodgeCooltime")]
public class CanDodgeCooltimeDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDodgeable dodgeable))
        {
            return dodgeable.CanDodge;
        }
        Debug.LogError("ERROR: Interface Not Found!!!");
        return false;
    }
}
