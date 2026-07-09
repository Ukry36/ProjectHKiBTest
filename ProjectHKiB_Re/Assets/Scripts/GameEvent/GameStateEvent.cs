using UnityEngine;

public class GameStateEvent : GameEvent
{
    [SerializeField] private EventSO _event;
    [SerializeField] private EventTargets _manualTargets;

    // start event by enabling controller update
    public override void TriggerEvent()
    {
        GameManager.instance.eventManager.StartEvent(_event, _manualTargets);
    }
}