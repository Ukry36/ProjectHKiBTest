using UnityEngine;
[CreateAssetMenu(fileName = "PlayFootstepAction", menuName = "State Machine/Action/Move/PlayFootstep")]
public class PlayFootstepAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IFootstep footstep))
        {
            footstep.PlayFootstepAudio(default);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
