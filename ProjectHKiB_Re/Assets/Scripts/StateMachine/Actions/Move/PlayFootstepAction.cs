using UnityEngine;
[CreateAssetMenu(fileName = "PlayFootstepAction", menuName = "State Machine/Action/Move/PlayFootstep")]
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
