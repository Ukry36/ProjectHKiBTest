using UnityEngine;
[CreateAssetMenu(fileName = "DialogueStartChoiceAction", menuName = "State Machine/Action/Dialogue/DialogueStartChoiceAction")]
public class DialogueStartChoiceAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDialogueable dialogue))
        {
            dialogue.StartChoice();
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
