using UnityEngine;
[CreateAssetMenu(fileName = "DialogueLineEndDecision", menuName = "State Machine/Decision/Dialogue/DialogueLineEndDecision")]
public class DialogueLineEndDecision : StateDecisionSO
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
