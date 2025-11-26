using UnityEngine;

[CreateAssetMenu(fileName = "Choice State", menuName = "State Machine/Dialogue States/Choice State", order = 1)]
public class ChoiceStateSO : StateSO
{
    public override void EnterState(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDialogueable dialogue))
        {
            dialogue.StartLine();
            dialogue.BindUpdateChoice();
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
        base.EnterState(stateController);
    }

    public override void ExitState(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDialogueable dialogue))
        {
            dialogue.UnBindUpdateChoice();
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
        base.ExitState(stateController);
    }
}
