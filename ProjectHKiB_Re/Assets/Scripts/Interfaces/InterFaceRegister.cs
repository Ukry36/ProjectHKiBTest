using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public abstract class InterfaceRegister : MonoBehaviour, IInterfaceRegistable
{
    public Dictionary<Type, IInitializable> Interfaces { get; set; } = new();
    public void RegisterInterface<T>(T implementation) where T : IInitializable
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
            //Debug.Log(interfaceModules[i]);
        }
    }

    public void InitializeModules()
    {
        IInitializable[] initializables = Interfaces.Values.ToArray();
        for (int i = 0; i < initializables.Length; i++)
        {
            initializables[i].Initialize();
        }
    }
}