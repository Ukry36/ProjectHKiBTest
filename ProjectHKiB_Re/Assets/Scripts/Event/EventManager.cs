using AYellowpaper.SerializedCollections;
using UnityEngine;

public class EventManager : StateController
{
    public SerializedDictionary<EventVariableSO, bool> boolEventVariables;
    public SerializedDictionary<EventVariableSO, int> intEventVariables;
    public SerializedDictionary<EventVariableSO, float> floatEventVariables;

    public void StartEvent(StateMachineSO stateMachine)
    {
        Initialize(stateMachine);
    }
}