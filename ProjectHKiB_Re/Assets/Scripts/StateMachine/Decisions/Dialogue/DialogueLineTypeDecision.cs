using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class DialogueLineTypeDecision : StateDecision
    {
        public StateSO type;
        public override bool Decide(StateController stateController)
        {
            if (stateController.TryGetInterface(out IDialogueable dialogue))
            {
                return dialogue.CheckLineType(type);
            }
            return false;
        }
    }
}