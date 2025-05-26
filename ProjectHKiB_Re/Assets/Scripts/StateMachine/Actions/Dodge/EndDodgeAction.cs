using UnityEngine;
[CreateAssetMenu(fileName = "EndDodge", menuName = "Scriptable Objects/State Machine/Action/Dodge/EndDodge")]
public class EndDodgeAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDodgeable dodgeable))
        {
            dodgeable.EndDodge();
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
