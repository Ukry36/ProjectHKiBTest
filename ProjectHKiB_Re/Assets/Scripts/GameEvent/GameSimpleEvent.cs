using UnityEngine;
using UnityEngine.Events;

public class GameSimpleEvent : GameEvent
{
    [SerializeField] private UnityEvent _function;

    public override void Initialize()
    {
        if (_trigger)
            _trigger.Initialize(this);
        EndEvent(null);
    }

    // start event by enabling controller update
    public override void TriggerEvent()
    {
        _function?.Invoke();
    }

    // end event by disabling controller update
    public override void EndEvent(Collider2D target)
    {
        if (target) CurrentTargets = new Collider2D[100];
    }
}