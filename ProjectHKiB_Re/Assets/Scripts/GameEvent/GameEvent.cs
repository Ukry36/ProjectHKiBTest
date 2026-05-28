using UnityEngine;

public abstract class GameEvent : MonoBehaviour, IEvent
{
    [SerializeField] protected GameEventTrigger _trigger;
    public Collider2D[] CurrentTargets { get; set; }
    public int TargetCount { get; set; } 

    public virtual void Start()
    {
        Initialize();
    }

    public virtual void Initialize()
    {
        if (_trigger)
            _trigger.Initialize(this);
        EndEvent(null);
        CurrentTargets = new Collider2D[100];
    }

    // you can register target transform and any required components in it here!
    public virtual void RegisterTarget(Collider2D[] targets, int cnt)
    {
        CurrentTargets = targets;
        TargetCount = cnt;
    }

    // start event by enabling controller update
    public abstract void TriggerEvent();

    // end event by disabling controller update
    // also reset target
    public abstract void EndEvent(Collider2D target);
    //{
    //    CurrentTarget = null;
    //}
}