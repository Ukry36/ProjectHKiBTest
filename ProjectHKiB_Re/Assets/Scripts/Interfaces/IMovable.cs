using System;
using System.Collections.Generic;
using UnityEngine;
public interface IMovableBase
{
    public float WalkSpeed { get; set; }
    public float SprintCoeff { get; set; }
    public LayerMask WallLayer { get; set; }
    public LayerMask CanPushLayer { get; set; }
    public float Mass { get; set; }
}

public class ExternalForce
{
    public Vector3 GetTotalForce()
    {
        Vector3 result = new();
        if (forces?.Count > 0)
        {
            foreach (var key in forces.Keys)
                result += forces[key];
        }
        return result;
    }

    public void SetForce(int ID, Vector3 force) => forces[ID] = force;
    
    public void AddForce(int ID, Vector3 force)
    {
        if (forces.ContainsKey(ID)) forces[ID] += force;
        else                        SetForce(ID, force);
    }

    private Dictionary<int, Vector3> forces = new();
}

public interface IMovable : IMovableBase, IInitializable
{
    public Vector3 Velocity { get; set; }
    public MovePoint MovePoint { get; set; }
    public Vector3 LastSetDir { get; set; }
    public bool IsKnockbackMove { get; set; }
    public bool IsWalking { get; set; }
    public Vector2 WalkingDir { get; set; }
    public bool IsSprinting { get; set; }
    public ExternalForce ExForce { get; set; }
    public BodyComponent[] BodyComponents { get; set; }
    public float ZPosition { get; set; }
    public void KnockBack(Vector3 dir, float strength);
    public void EndKnockbackEarly();
    public void KnockBackEndCallback();
}