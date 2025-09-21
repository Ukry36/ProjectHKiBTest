using UnityEngine;
[CreateAssetMenu(fileName = "KeepDodgeEnded", menuName = "State Machine/Decision/KeepDodgeEnded")]
public class KeepDodgeEndedDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDodgeable dodgeable))
        {
            return dodgeable.CheckKeepDodgeEnded();
        }
        Debug.LogError("ERROR: Interface Not Found!!!");
        return false;
    }
}
