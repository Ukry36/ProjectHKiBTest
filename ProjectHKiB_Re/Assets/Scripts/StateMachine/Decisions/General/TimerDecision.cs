using NaughtyAttributes;
using UnityEngine;
[CreateAssetMenu(fileName = "TimerDecision", menuName = "State Machine/Decision/General/TimerDecision")]
public class TimerDecision : StateDecisionSO
{
    [MinValue(0)][MaxValue(9)]public int timerID;
    public override bool Decide(StateController stateController)
    {
        return stateController.Timers[timerID].IsCooltimeEnded;
    }
}
