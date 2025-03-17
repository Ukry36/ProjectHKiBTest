using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class CustomVariable<T>
{
    [SerializeField] private T value;
    [SerializeField] public UnityEvent<T> OnValueChanged;
    public T Value
    {
        get => value;
        set
        {
            this.value = value;
            OnValueChanged?.Invoke(value);
        }
    }
}