using System.Collections.Generic;
using UnityEngine;
using System;

public class StateController : InterfaceRegister
{
    [HideInInspector] public CustomVariableSets customVariables = new();
    [NaughtyAttributes.ReadOnly][SerializeField] protected StateSO _currentState;
    public StateSO CurrentState
    {
        get => _currentState;
        private set
        {
            if (value != _currentState)
                _currentState = value;
            //Debug.Log(_currentState);
        }
    }
    [HideInInspector] public List<Coroutine> FrameActionSequences = new(10);
    [HideInInspector] public List<Coroutine> TransitionSequences = new(10);
    [HideInInspector] public List<bool> TransitionConditions = new(10);

    public virtual void Awake()
    {
        for (int i = 0; i < 10; i++)
        {
            FrameActionSequences.Add(null);
            TransitionSequences.Add(null);
            TransitionConditions.Add(false);
        }
    }

    public virtual void Start()
    {
        Initialize();
    }

    public virtual void Initialize()
    {
        RegisterModules(transform);
    }

    public virtual void ChangeState(StateSO state)
    {
        CurrentState.ExitState(this);
        CurrentState = state;
        CurrentState.EnterState(this);
    }

    public virtual void UpdateState()
    {
        CurrentState.UpdateState(this);
        //Debug.Log("CheckTransition: " + _currentState.name);
        CurrentState.CheckTransition(this);
    }

    public void Update()
    {
        if (this.enabled && CurrentState)
            UpdateState();
    }

    public void Initialize(StateMachineSO stateMachine)
    {
        ResetStateMachine(stateMachine);
        customVariables = stateMachine.customVariables;
        //////
        ///  HAVE TO FIX THIS NOT TO DEEP REFERENCE CUSTOMVARS!!!
        //////
    }

    public void ResetStateMachine(StateMachineSO stateMachine)
    {
        if (stateMachine == null)
        {
            Debug.LogError("ERROR: StateMachine Missing!!!");
            return;
        }
        CurrentState = stateMachine.initialState;
        CurrentState.EnterState(this);
    }

    public void EliminateStateMachine()
    {
        if(CurrentState) CurrentState.ExitState(this);
        CurrentState = null;
        StopAllCoroutines();
    }

    public void SetBoolParameterTrue(string name)
    {
        if (!customVariables.boolVariables.ContainsKey(name))
        {
            Debug.LogWarning("Warning: Generated missing variable: " + name);
            customVariables.boolVariables[name] = new() { Value = true };
        }
        else
            customVariables.boolVariables[name].Value = true;
    }

    public void SetBoolParameterFalse(string name)
    {
        if (!customVariables.boolVariables.ContainsKey(name))
        {
            Debug.LogWarning("Warning: Generated missing variable: " + name);
            customVariables.boolVariables[name] = new() { Value = false };
        }
        else
            customVariables.boolVariables[name].Value = false;
    }

    public void SetIntParameter(string name, int value)
    {
        customVariables.intVariables[name].Value = value;
    }

    public bool GetBoolParameter(string name)
    {
        if (!customVariables.boolVariables.ContainsKey(name))
        {
            Debug.LogWarning("Warning: Generated missing variable: " + name);
            customVariables.boolVariables[name] = new() { Value = false };
        }

        return customVariables.boolVariables[name].Value;
    }
}