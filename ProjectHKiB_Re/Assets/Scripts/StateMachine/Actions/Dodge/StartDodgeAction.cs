using UnityEngine;
[CreateAssetMenu(fileName = "StartDodge", menuName = "Scriptable Objects/State Machine/Action/Dodge/StartDodge")]
public class StartDodgeAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDodgeable dodgeable))
        {
            dodgeable.StartDodge();
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
