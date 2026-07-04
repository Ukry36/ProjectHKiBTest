using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class DialogueStartLineAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IDialogueable dialogue))
            {
                dialogue.StartLine();
            }
            else Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}