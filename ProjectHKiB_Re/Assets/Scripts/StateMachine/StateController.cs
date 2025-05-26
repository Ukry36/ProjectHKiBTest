using System.Collections.Generic;
using UnityEngine;
using System;

public class StateController : MonoBehaviour, IInterfaceRegistable
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
    [HideInInspector] public List<Coroutine> FrameActionSequences = new(36);
    [HideInInspector] public List<Coroutine> TransitionSequences = new(36);
    [HideInInspector] public List<bool> TransitionConditions = new(36);
    public Dictionary<Type, object> Interfaces { get; set; } = new();

    protected virtual void Awake()
    {
        for (int i = 0; i < 36; i++)
        {
            FrameActionSequences.Add(null);
            TransitionSequences.Add(null);
            TransitionConditions.Add(false);
        }
    }

    public void RegisterInterface<T>(T implementation) where T : class
    {
        Interfaces[typeof(T)] = implementation;
    }

    public T GetInterface<T>() where T : class
    {
        if (Interfaces.TryGetValue(typeof(T), out var implementation))
        {
            return implementation as T;
        }
        return null;
    }

    public bool TryGetInterface<T>(out T item) where T : class
    {
        if (Interfaces.TryGetValue(typeof(T), out var implementation))
        {
            item = implementation as T;
            return true;
        }
        else
        {
            item = default;
            return false;
        }
    }

    public void RegisterModules(Transform transform)
    {
        InterfaceModule[] interfaceModules = transform.GetComponents<InterfaceModule>();
        for (int i = 0; i < interfaceModules.Length; i++)
        {
            interfaceModules[i].Register(this);
        }
    }

    public virtual void ChangeState(StateSO state)
    {
        CurrentState.ExitState(this);
        CurrentState = state;
        CurrentState.EnterState(this);
    }

    public void UpdateState()
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