using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class PlayFootstepAction : StateAction
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
}