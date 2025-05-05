using UnityEngine;
[CreateAssetMenu(fileName = "PlayFootstepAction", menuName = "Scriptable Objects/State Machine/Action/PlayFootstep", order = 3)]
public class PlayFootstepAction : StateActionSO
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
