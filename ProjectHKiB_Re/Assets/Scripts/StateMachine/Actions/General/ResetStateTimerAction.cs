using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class ResetStateTimerAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            stateController.CurrentState.ResetStateTimer(stateController);
        }
    }
}