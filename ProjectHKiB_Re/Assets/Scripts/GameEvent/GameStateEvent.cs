using UnityEngine;

public class GameStateEvent : GameEvent
{
    [SerializeField] private StateController _stateController;
    [SerializeField] private StateMachineSO _stateMachine;

    public override void Initialize()
    {
        if (_trigger)
            _trigger.Initialize(this);
        _stateController.RegisterModules(this.transform);
        _stateController.RegisterInterface<IEvent>(this);
        EndEvent();
    }

    // start event by enabling controller update
    public override void TriggerEvent()
    {
        _stateController.enabled = true;
        _stateController.ResetStateMachine(_stateMachine);
    }

    // end event by disabling controller update
    // also reset target
    public override void EndEvent()
    {
        CurrentTarget = null;
        _stateController.EliminateStateMachine();
        _stateController.enabled = false;
    }
}