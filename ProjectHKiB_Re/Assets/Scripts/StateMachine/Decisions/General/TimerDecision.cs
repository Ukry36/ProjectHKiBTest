using NaughtyAttributes;
namespace StateMachine
{
    [System.Serializable]
    public class TimerDecision : StateDecision
    {
        [MinValue(0)][MaxValue(9)] public int timerID;
        public override bool Decide(StateController stateController)
        {
            return stateController.Timers[timerID].IsCooltimeEnded;
        }
    }
}