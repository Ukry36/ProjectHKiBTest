using UnityEngine;

namespace StateMachine
{
    [System.Serializable]
    public class DialogueShowLineAction : StateAction
    {
        public Line line;
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IDialogueable dialogue))
            {
                dialogue.StartLine(line);
            }
            else Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}
