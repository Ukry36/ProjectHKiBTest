using UnityEngine;

public abstract class GameEvent : MonoBehaviour, IEvent
{
    [SerializeField] protected EventTrigger _trigger;
    public Transform CurrentTarget { get; set; }

    public virtual void Start()
    {
        Initialize();
    }

    public virtual void Initialize()
    {
        if (_trigger)
            _trigger.Initialize(this);
        EndEvent();
    }

    // you can register target transform and any required components in it here!
    // event will run for only one entity
    public virtual void RegisterTarget(Transform transform)
    {
        CurrentTarget = transform;
    }

    // start event by enabling controller update
    public abstract void TriggerEvent();

    // end event by disabling controller update
    // also reset target
    public abstract void EndEvent();
    //{
    //    CurrentTarget = null;
    //}
}