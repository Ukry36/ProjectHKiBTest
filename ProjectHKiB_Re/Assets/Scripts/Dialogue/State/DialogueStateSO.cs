using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue State", menuName = "State Machine/Dialogue States/Dialogue State", order = 0)]
public class DialogueStateSO : StateSO
{
    public override void EnterState(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDialogueable dialogue))
        {
            dialogue.StartLine();
            dialogue.BindUpdateLine();
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
        base.EnterState(stateController);
    }

    public override void ExitState(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDialogueable dialogue))
        {
            dialogue.UnBindUpdateLine();
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
        base.ExitState(stateController);
    }
}