using UnityEngine;

public class GameEvent : MonoBehaviour, IEvent
{
    [SerializeField] private StateController _stateController;
    [SerializeField] private EventTrigger _trigger;
    [SerializeField] private StateMachineSO _stateMachine;
    public Transform CurrentTarget { get; set; }


    public virtual void Start()
    {
        Initialize();
    }

    public virtual void Initialize()
    {
        if (_trigger)
            _trigger.Initialize(this);
        _stateController.RegisterModules(this.transform);
        _stateController.RegisterInterface<IEvent>(this);
        EndEvent();
    }

    // you can register target transform and any required components in it here!
    // event will run for only one entity
    public virtual void RegisterTarget(Transform transform)
    {
        CurrentTarget = transform;
    }

    // start event by enabling controller update
    public virtual void TriggerEvent()
    {
        _stateController.enabled = true;
        _stateController.ResetStateMachine(_stateMachine);
    }

    // end event by disabling controller update
    // also reset target
    public virtual void EndEvent()
    {
        CurrentTarget = null;
        _stateController.EliminateStateMachine();
        _stateController.enabled = false;
    }
}

internal class EventEventRegisterModule
{
}