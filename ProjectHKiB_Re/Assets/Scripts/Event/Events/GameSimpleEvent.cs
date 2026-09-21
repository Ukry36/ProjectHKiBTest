using UnityEngine;
using UnityEngine.Events;

public class GameSimpleEvent : GameEvent
{
    [SerializeField] private UnityEvent _function;

    // start event by enabling controller update
    public override void TriggerEvent()
    {
        _function?.Invoke();
    }
}