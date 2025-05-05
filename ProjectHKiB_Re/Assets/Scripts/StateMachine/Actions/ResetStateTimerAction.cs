using UnityEngine;
[CreateAssetMenu(fileName = "ResetStateTimerAction", menuName = "Scriptable Objects/State Machine/Action/ResetStateTimer", order = 3)]
public class ResetStateTimerAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        stateController.CurrentState.ResetStateTimer(stateController);
    }
}
