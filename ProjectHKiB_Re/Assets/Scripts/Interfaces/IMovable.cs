using System;
using System.Collections.Generic;
using UnityEngine;
public interface IMovableBase
{
    public float Speed { get; set; }
    public float SprintCoeff { get; set; }
    public LayerMask WallLayer { get; set; }
    public LayerMask CanPushLayer { get; set; }
    public float Mass { get; set; }
}

public class ExternalForce
{
    public Vector3 GetSumForce()
    {
        Vector3 result = new();
        if (Forces?.Count > 0)
        {
            foreach (var key in Forces.Keys)
                result += Forces[key];
        }
        return result;
    }

    public void SetForce(int ID, Vector3 force)
    {
        Forces[ID] = force;
    }

    public Dictionary<int, Vector3> Forces { get; set; }
    public ExternalForce(bool use)
    {
        Forces = new();
    }
}

public interface IMovable : IMovableBase, IInitializable
{
    public Vector3 Velocity { get; set; }
    public MovePoint MovePoint { get; set; }
    public Vector3 LastSetDir { get; set; }
    public bool IsKnockbackMove { get; set; }
    public bool IsSprinting { get; set; }
    public ExternalForce ExForce { get; set; }
    public BodyComponent[] BodyComponents { get; set; }
    public float ZLevel { get; set; }
    public void KnockBack(Vector3 dir, float strength);
    public void EndKnockbackEarly();
    public void KnockBackEndCallback();
}