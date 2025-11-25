using UnityEngine;
[CreateAssetMenu(fileName = "DialogueEndDecision", menuName = "State Machine/Decision/Dialogue/DialogueEndDecision")]
public class DialogueEndDecision : StateDecisionSO
{
    public override bool Decide(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDialogueable dialogue))
        {
            return dialogue.CheckDialogueEnd();
        }
        return false;
    }
}
