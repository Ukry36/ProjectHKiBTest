using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class StatContainer
{
    [SerializeField] public float baseValue;
    [SerializeField] public UnityEvent<float> OnValueDecreased;
    [SerializeField] public UnityEvent<float> OnValueIncreased;
    [HideInInspector] public float additionalBuff;
    [HideInInspector] public float proportionalBuff;
    public float Value
    {
        get
        {
            return (baseValue + additionalBuff) * (1f + proportionalBuff);
        }
        set
        {
            if (value > baseValue)
                OnValueIncreased?.Invoke(value);
            else if (value < baseValue)
                OnValueDecreased?.Invoke(value);

            baseValue = value;
        }
    }

    public StatContainer(StatContainer statContainer)
    {
        this.OnValueDecreased = statContainer.OnValueDecreased;
        this.OnValueIncreased = statContainer.OnValueIncreased;
        this.additionalBuff = statContainer.additionalBuff;
        this.proportionalBuff = statContainer.proportionalBuff;
        this.baseValue = statContainer.baseValue;
    }
}