using AYellowpaper.SerializedCollections;
using UnityEngine;

public class EventManager : StateController
{
    public SerializedDictionary<EventVariableSO, bool> boolEventVariables;
    public SerializedDictionary<EventVariableSO, int> intEventVariables;
    public SerializedDictionary<EventVariableSO, float> floatEventVariables;

    public EventSO currentEvent;
    public EventBlock currentEventBlock;

    public void StartEvent(EventSO currentEvent)
    {
        this.currentEvent = currentEvent;
        currentEventBlock = currentEvent.rootEventBlock;
        CurrentState = currentEventBlock.state;
        CurrentState.EnterState(this);
    }

    public void ProgressEventBlock()
    {
        for (int i = 0; i < currentEventBlock.nextEvents.Length; i++)
        {
            EventFlagSO[] flags = currentEventBlock.nextEvents[i].flags;
            bool flag = true;
            for (int j = 0; j < flags.Length; j++)
            {
                flag &= flags[j].GetFlag(this);
            }
            if (flag) 
            {
                currentEventBlock = currentEventBlock.nextEvents[i].nextEvent;
                break;
            }
        }
        ChangeState(currentEventBlock.state);
    }

    public override void UpdateState()
    {
        CurrentState.UpdateState(this);
    }
}