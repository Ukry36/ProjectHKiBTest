using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class DialogueStartAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IDialogueable dialogue))
            {
                dialogue.StartDialogue();
            }
            else Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}