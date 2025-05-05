using UnityEngine;
[CreateAssetMenu(fileName = "TriggerFootstep", menuName = "Scriptable Objects/State Machine/Action/TriggerFootstepAction", order = 3)]
public class TriggerFootstepAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IMovable movable))
        {
            movable.FootstepController.PlayFootstepAudio(default);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}
