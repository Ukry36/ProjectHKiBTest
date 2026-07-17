using UnityEngine;

public abstract class GameEvent : MonoBehaviour
{
    [SerializeField] protected GameEventTrigger _trigger;

    public virtual void Start()
    {
        Initialize();
    }

    public virtual void Initialize()
    {
        if (_trigger)
            _trigger.Initialize(this);
    }

    // start event by enabling controller update
    public abstract void TriggerEvent();
}