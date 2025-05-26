using UnityEngine;
[CreateAssetMenu(fileName = "ResetStateTimerAction", menuName = "Scriptable Objects/State Machine/Action/General/ResetStateTimer")]
public class ResetStateTimerAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        stateController.CurrentState.ResetStateTimer(stateController);
    }
}
