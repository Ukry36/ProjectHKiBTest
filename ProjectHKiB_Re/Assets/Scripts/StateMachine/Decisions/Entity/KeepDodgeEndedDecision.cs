using UnityEngine;
[CreateAssetMenu(fileName = "KeepDodgeEnded", menuName = "Scriptable Objects/State Machine/Decision/KeepDodgeEnded", order = 4)]
public class KeepDodgeEndedDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDodgeable dodgeable))
        {
            return dodgeable.IsKeepDodgeEnded();
        }
        Debug.LogError("ERROR: Interface Not Found!!!");
        return false;
    }
}
