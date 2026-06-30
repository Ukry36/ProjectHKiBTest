using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class DialogueLineEndDecision : StateDecision
    {
        public override bool Decide(StateController stateController)
        {
            if (stateController.TryGetInterface(out IDialogueable dialogue))
            {
                return dialogue.CheckLineEnd();
            }
            return false;
        }
    }
}