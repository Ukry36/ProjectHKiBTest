using System.Collections.Generic;
using UnityEngine;

public abstract class Entity : StateController
{
    public override void Start()
    {
        Initialize();
    }
    public override void Initialize()
    {
        RegisterModules(transform);
        //HealthController.Initialize(this, BaseMaxHP);
        // MovementController.Initialize();
    }
}