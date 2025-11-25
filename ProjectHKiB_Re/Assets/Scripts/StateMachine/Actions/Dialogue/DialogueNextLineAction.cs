using UnityEngine;
[CreateAssetMenu(fileName = "DialogueNextLineAction", menuName = "State Machine/Action/Dialogue/DialogueNextLineAction")]
public class DialogueNextLineAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDialogueable dialogue))
        {
            dialogue.NextLine();
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
