using UnityEngine;

public class GameStateEvent : GameEvent
{
    [SerializeField] private EventSO _event;
    [SerializeField] private EventTargets _manualTargets;

    public override void Initialize()
    {
        if (_trigger)
            _trigger.Initialize(this);
        EndEvent(null);
    }

    // start event by enabling controller update
    public override void TriggerEvent()
    {
        GameManager.instance.eventManager.StartEvent(_event, _manualTargets);
    }

    // end event by disabling controller update
    // also reset target
    public override void EndEvent(Collider2D target)
    {
        if (target) CurrentTargets = new Collider2D[100];
    }
}