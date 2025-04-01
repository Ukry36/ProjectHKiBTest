using System.Collections.Generic;
using UnityEngine;
using System;

public class StateController : MonoBehaviour
{
    [HideInInspector] public CustomVariableSets customVariables = new();
    [SerializeField] private StateSO _currentState;
    [HideInInspector] public bool animationEndTrigger;
    [SerializeField] private AnimationController _animationController;
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
        if (_animationController)
            _animationController.Play(animationName, directionDependent);
        else
            Debug.LogWarning("Warning: animationController missing!!!");
    }

    public void SetAnimationDirection(Vector2 dir)
    {
        if (_animationController)
            _animationController.SetAnimationDirection(dir);
        else
            Debug.LogWarning("Warning: animationController missing!!!");
    }
}