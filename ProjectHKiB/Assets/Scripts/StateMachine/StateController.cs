using UnityEngine;

public class StateController : MonoBehaviour
{
    public CustomVariableSets customVariables = new CustomVariableSets();
    private StateSO _currentState;
    [HideInInspector] public StateSO remainState;
    [HideInInspector] public bool animationEndTrigger;

    public virtual void Update()
    {
        _currentState.UpdateState(this);
    }

    public virtual void InitializeState(StateMachineSO stateMachine)
    {
        _currentState = stateMachine.initialState;
    }

    public void ChangeState(StateSO state)
    {
        animationEndTrigger = false;
        _currentState.ExitState(this);
        remainState = _currentState = state;
        _currentState.EnterState(this);
    }
}