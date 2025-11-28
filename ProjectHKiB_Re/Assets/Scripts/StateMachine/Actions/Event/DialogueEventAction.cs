using UnityEngine;
[CreateAssetMenu(fileName = "DialogueEventAction", menuName = "State Machine/Action/Event/DialogueEventAction")]
public class DialogueEventAction : StateActionSO
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
