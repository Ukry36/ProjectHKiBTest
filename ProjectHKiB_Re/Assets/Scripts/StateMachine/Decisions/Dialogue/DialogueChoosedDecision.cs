using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class DialogueChoosedDecision : StateDecision
    {
        public int choice;
        public override bool Decide(StateController stateController)
        {
            if (stateController.TryGetInterface(out IDialogueable dialogueable))
            {
                return dialogueable.ChoicedNum == choice;
            }
            Debug.LogError("ERROR: Interface Not Found!!!");
            return false;
        }
    }
}