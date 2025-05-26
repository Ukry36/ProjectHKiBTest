using UnityEngine;
[CreateAssetMenu(fileName = "StartKeepDodge", menuName = "Scriptable Objects/State Machine/Action/Dodge/StartKeepDodge")]
public class StartKeepDodgeAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDodgeable dodgeable))
        {
            dodgeable.StartKeepDodge();
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
