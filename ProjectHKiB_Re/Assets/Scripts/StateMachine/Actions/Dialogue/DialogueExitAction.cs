using UnityEngine;
[CreateAssetMenu(fileName = "DialogueExitAction", menuName = "State Machine/Action/Dialogue/DialogueExitAction")]
public class DialogueExitAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDialogueable dialogue))
        {
            dialogue.ExitDialogue();
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
