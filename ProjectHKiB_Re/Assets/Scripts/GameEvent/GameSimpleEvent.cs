using UnityEngine;
using UnityEngine.Events;

public class GameSimpleEvent : GameEvent
{
    [SerializeField] private UnityEvent _function;

    public override void Initialize()
    {
        if (_trigger)
            _trigger.Initialize(this);
        EndEvent();
    }

    // start event by enabling controller update
    public override void TriggerEvent()
    {
        _function?.Invoke();
    }

    // end event by disabling controller update
    // also reset target
    public override void EndEvent()
    {
        CurrentTarget = null;
    }
}