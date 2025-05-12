
using UnityEngine;

public class EventStateController : StateController, IEventController
{
    [field: SerializeField] public Transform CurrentTarget { get; set; }
    [field: SerializeField] public StateMachineSO StateMachine { get; set; }
    [field: SerializeField] public EventTrigger Trigger { get; set; }

    public void Start()
    {
        Initialize();
    }

    public void InitializeTrigger()
    {
        Trigger.EventController = this;
    }

    public void Initialize()
    {
        InitializeTrigger();
        Initialize(StateMachine);
        RegisterInterface<IEventController>(this);
        EndEvent();
    }

    public virtual void RegisterTarget(Transform transform)
    {
        CurrentTarget = transform;
    }

    public void TriggerEvent()
    {
        this.enabled = true;
    }

    public void EndEvent()
    {
        CurrentTarget = null;
        this.enabled = false;
    }
}