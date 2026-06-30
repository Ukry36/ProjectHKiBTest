using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class DialogueExitAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IDialogueable dialogue))
            {
                dialogue.ExitDialogue();
            }
            else Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}