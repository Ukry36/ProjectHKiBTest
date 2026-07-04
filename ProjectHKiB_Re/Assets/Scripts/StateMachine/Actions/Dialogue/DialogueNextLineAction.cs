using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class DialogueNextLineAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IDialogueable dialogue))
            {
                dialogue.NextLine();
            }
            else Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}