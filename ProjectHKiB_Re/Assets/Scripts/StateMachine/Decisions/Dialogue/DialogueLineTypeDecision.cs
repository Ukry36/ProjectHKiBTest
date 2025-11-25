using UnityEngine;
[CreateAssetMenu(fileName = "DialogueLineTypeDecision", menuName = "State Machine/Decision/Dialogue/DialogueLineTypeDecision")]
public class DialogueLineTypeDecision : StateDecisionSO
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
