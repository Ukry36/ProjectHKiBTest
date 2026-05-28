using System.Collections.Generic;
using UnityEngine;

public class ForceFieldEvent : GameEvent
{
    public Vector2 dir;
    public float strength;
    public bool isCenter;
    public bool yeet;

    public override void Initialize()
    {
        if (_trigger)
            _trigger.Initialize(this);
        EndEvent(null);
    }

    // start event by enabling controller update
    public override void TriggerEvent()
    {
        if (TargetCount < 1) return;
        if (yeet) dir = Quaternion.Euler(0, 0, -1) * dir;

        for (int i = 0; i < TargetCount; i++)
        {
            if (CurrentTargets[i].TryGetComponent(out IPhysics physics))
            {
                Vector3 force = isCenter
                    ? (transform.position - physics.Position).normalized * strength
                    : (Vector3)(dir.normalized * strength);

                physics.ExForce += force;
            }
        } 
    }

    // end event by disabling controller update
    // also reset target
    public override void EndEvent(Collider2D target)
    {
        if (target) CurrentTargets = new Collider2D[100];
    }
}