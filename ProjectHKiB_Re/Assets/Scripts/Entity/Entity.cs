using System.Collections.Generic;
using UnityEngine;

public abstract class Entity : StateController
{
    public override void Start()
    {
        base.Start();
        Initialize();
    }
    public virtual void Initialize()
    {
        //RegisterModules(transform);
        //HealthController.Initialize(this, BaseMaxHP);
        // MovementController.Initialize();
    }
}