using System.Collections.Generic;
using UnityEngine;

public abstract class Entity : StateController
{
    public override void Awake()
    {
        base.Awake();
        Initialize();
    }
    public virtual void Initialize()
    {
        RegisterModules(transform);
        //HealthController.Initialize(this, BaseMaxHP);
        // MovementController.Initialize();
    }
}