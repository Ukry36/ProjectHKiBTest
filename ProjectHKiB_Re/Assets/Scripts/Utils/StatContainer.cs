using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class StatContainer
{
    [SerializeField] private float _value;
    [SerializeField] public UnityEvent<float> OnValueDecreased;
    [SerializeField] public UnityEvent<float> OnValueIncreased;
    [HideInInspector] public float additionalBuff;
    [HideInInspector] public float proportionalBuff;
    public float Value
    {
        get => _value;
        set
        {
            if (value > _value)
                OnValueIncreased?.Invoke(value);
            else if (value < _value)
                OnValueDecreased?.Invoke(value);

            _value = value;
        }
    }
}