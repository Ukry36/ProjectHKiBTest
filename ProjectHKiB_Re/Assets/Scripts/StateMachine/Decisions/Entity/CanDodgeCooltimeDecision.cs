using UnityEngine;
[CreateAssetMenu(fileName = "CanDodgeCooltime", menuName = "Scriptable Objects/State Machine/Decision/CanDodgeCooltime")]
public class CanDodgeCooltimeDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDodgeable dodgeable))
        {
            return dodgeable.CanDodge();
        }
        return false;
    }
}
