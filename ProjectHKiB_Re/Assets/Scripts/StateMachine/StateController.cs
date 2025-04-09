using System.Collections.Generic;
using UnityEngine;
using System;

public class StateController : MonoBehaviour
{
    [HideInInspector] public CustomVariableSets customVariables = new();
    [SerializeField] private StateSO _currentState;
    [HideInInspector] public bool animationEndTrigger;
    public AnimationController animationController;
    [SerializeField] private AttackController _attackController;
    [HideInInspector] public Vector2 velocity;
    private Vector3 prevPos;




    private readonly Dictionary<Type, object> _interfaces = new();

    public void RegisterInterface<T>(T implementation) where T : class
    {
        _interfaces[typeof(T)] = implementation;
    }

    public T GetInterface<T>() where T : class
    {
        if (_interfaces.TryGetValue(typeof(T), out var implementation))
        {
            return implementation as T;
        }
        return null;
    }

    public bool TryGetInterFace<T>(out T item) where T : class
    {
        if (_interfaces.TryGetValue(typeof(T), out var implementation))
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

    public void Start()
    {
        prevPos = transform.position;
    }

    public void Update()
    {
        velocity = transform.position - prevPos;
        velocity.Normalize();
        prevPos = transform.position;
        _currentState.UpdateState(this);
        _currentState.CheckTransition(this);
    }

    public void Initialize(StateMachineSO stateMachine)
    {
        _currentState = stateMachine.initialState;
        customVariables = stateMachine.customVariables;
        //////
        ///  HAVE TO FIX THIS NOT TO DEEP REFERENCE CUSTOMVARS!!!
        //////
    }

    public void ChangeState(StateSO state)
    {
        animationEndTrigger = false;
        _currentState.ExitState(this);
        state.EnterState(this);
        _currentState = state;
    }

    public void PlayStateAnimation(string animationName, bool directionDependent)
    {
        if (animationController)
            animationController.Play(animationName, directionDependent);
        else
            Debug.LogWarning("Warning: animationController missing!!!");
    }

    public void AnimationEndTrigger()
    => animationEndTrigger = true;

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