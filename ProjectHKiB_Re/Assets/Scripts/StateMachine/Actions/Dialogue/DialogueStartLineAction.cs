using UnityEngine;
[CreateAssetMenu(fileName = "DialogueStartLineAction", menuName = "State Machine/Action/Dialogue/DialogueStartLineAction")]
public class DialogueStartLineAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDialogueable dialogue))
        {
            dialogue.StartLine();
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
