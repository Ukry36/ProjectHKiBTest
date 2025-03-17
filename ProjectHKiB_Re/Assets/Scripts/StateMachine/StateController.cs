using System.Collections.Generic;
using UnityEngine;
using System;

public class StateController : MonoBehaviour
{
    public CustomVariableSets customVariables = new();
    [SerializeField] private StateSO _currentState;
    [HideInInspector] public StateSO remainState;
    [HideInInspector] public bool animationEndTrigger;
    public AnimationController animationController;
    public Vector2 velocity;
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

    public void InitializeState(StateMachineSO stateMachine)
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